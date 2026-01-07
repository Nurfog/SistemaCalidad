using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Services;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using Serilog;
using SistemaCalidad.Api.Middleware;
using SistemaCalidad.Api.Hubs;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "frontend_app" // Carpeta personalizada para el Frontend (evita conflicto con wwwroot del servidor)
});

builder.Host.UseSerilog();

// Cargar variables desde .env al entorno del proceso
// Usamos ContentRootPath que es más seguro en IIS
var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
Console.WriteLine($"[Startup] Buscando archivo .env en: {envPath}");

if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
    Console.WriteLine("[Startup] Archivo .env cargado.");
}
else
{
    Console.WriteLine("[Startup] ⚠️ NO SE ENCONTRO EL ARCHIVO .env");
}

// Reemplazo explícito de la cadena de conexión (Más seguro que el loop genérico)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "";
    // Corregido: .env usa DB_PASS, no DB_PASSWORD
    var dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? "";
    
    // Diagnóstico de contraseña (Seguro)
    var passLog = string.IsNullOrEmpty(dbPass) ? "VACIO" : $"Longitud: {dbPass.Length}, Inicia: '{dbPass.FirstOrDefault()}', Termina: '{dbPass.LastOrDefault()}'";

    Console.WriteLine($"[Startup] Variables cargadas - Host: {dbHost}, Port: {dbPort}, DB: {dbName}, User: {dbUser}");
    Console.WriteLine($"[Startup] Password Debug: {passLog}");

    connectionString = connectionString.Replace("{DB_HOST}", dbHost)
                                     .Replace("{DB_NAME}", dbName)
                                     .Replace("{DB_USER}", dbUser)
                                     .Replace("{DB_PASS}", dbPass);
    
    // Sobrescribir la configuración en memoria
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// MySQL Connection
// Configure JWT Authentication - Leer desde variables de entorno
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"] ?? "Clave_Por_Defecto_Muy_Larga_1234567890!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"];
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"];

// Actualizar configuración en memoria
builder.Configuration["Jwt:Key"] = jwtKey;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;

Console.WriteLine($"[Startup] JWT Key Length: {jwtKey?.Length ?? 0} caracteres");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


Console.WriteLine($"Iniciando en modo: {(builder.Environment.IsDevelopment() ? "Desarrollo (AWS)" : "Producción (Localhost)")}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

// Health Checks
builder.Services.AddHealthChecks()
    .AddMySql(connectionString!, name: "Base de Datos");
// Nota: S3 Health check se puede agregar con paquetes adicionales, 
// por ahora usaremos una validación custom en el controlador de status.

// File Storage Service
builder.Services.AddHttpContextAccessor();

var useS3 = builder.Configuration.GetValue<bool>("FileStorage:UseS3");
if (useS3)
{
    // Configurar AWS SDK
    var awsOptions = builder.Configuration.GetAWSOptions();
    
    // Obtener variables de entorno explícitamente (Usando las claves del .env)
    var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? builder.Configuration["FileStorage:S3:Region"];
    var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY") ?? builder.Configuration["FileStorage:S3:AccessKey"];
    var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY") ?? builder.Configuration["FileStorage:S3:SecretKey"];
    var bucketName = Environment.GetEnvironmentVariable("AWS_S3_BUCKET") ?? builder.Configuration["FileStorage:S3:BucketName"];

    // Actualizar configuración en memoria para otros usos
    builder.Configuration["FileStorage:S3:BucketName"] = bucketName;

    awsOptions.Region = Amazon.RegionEndpoint.GetBySystemName(awsRegion);
    awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(awsAccessKey, awsSecretKey);
    
    builder.Services.AddDefaultAWSOptions(awsOptions);
    builder.Services.AddAWSService<IAmazonS3>();
    builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
    Console.WriteLine($"💾 Almacenamiento configurado en: Amazon S3 (Bucket: {builder.Configuration["FileStorage:S3:BucketName"]}, Region: {awsOptions.Region.SystemName})");
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    Console.WriteLine("💾 Almacenamiento configurado en: Local (Carpeta Storage)");
}

// Configuración de IA (Google Gemini / OpenCCB)
var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_AI_KEY");
var aiApiUrl = Environment.GetEnvironmentVariable("AI_API_URL");

if (!string.IsNullOrEmpty(googleApiKey)) builder.Configuration["GoogleAI:ApiKey"] = googleApiKey;
if (!string.IsNullOrEmpty(aiApiUrl)) 
{
    builder.Configuration["AI_API_URL"] = aiApiUrl;
    Console.WriteLine($"[Startup] 🤖 IA configurada (OpenCCB AI via: {aiApiUrl})");
}
else 
{
    Console.WriteLine("[Startup] ⚠️ ADVERTENCIA: No se detectó AI_API_URL. Usando localhost por defecto.");
}

builder.Services.AddSingleton<TemplateService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IWatermarkService, WatermarkService>();
builder.Services.AddScoped<IDocumentConverterService, DocumentConverterService>();
builder.Services.AddHttpClient<IIAService, IAService>(client => 
{
    client.Timeout = TimeSpan.FromMinutes(10); // Aumentar hasta 10 minutos por posibles retrasos en hardware local
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .WithOrigins("http://localhost:5173", "http://localhost:5000", "http://localhost:5156", "https://calidad.norteamericano.cl")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition"));
});

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Dashboard Premium para probar la API
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseMiddleware<UserStatusMiddleware>(); // Validación de estado en tiempo real
app.UseAuthorization();

// Servir archivos estáticos (Frontend React)
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<NotificacionHub>("/hub/notificaciones");

// Fallback para SPA (Cualquier ruta que no sea API va a index.html)
app.MapFallbackToFile("index.html");

// La base de datos se maneja manualmente via scripts SQL

app.Run();
