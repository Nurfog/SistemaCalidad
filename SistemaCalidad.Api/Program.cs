using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Services;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables desde .env al entorno del proceso
DotNetEnv.Env.Load();

// Reemplazar placeholders en la configuración con variables de entorno reales
var configuracionDict = new Dictionary<string, string>();
var envVars = Environment.GetEnvironmentVariables();

foreach (System.Collections.DictionaryEntry envVar in envVars)
{
    string key = envVar.Key.ToString()!;
    string value = envVar.Value?.ToString() ?? "";

    foreach (var config in builder.Configuration.AsEnumerable())
    {
        if (config.Value != null && config.Value.Contains("{" + key + "}"))
        {
            configuracionDict[config.Key] = config.Value.Replace("{" + key + "}", value);
        }
    }
}
builder.Configuration.AddInMemoryCollection(configuracionDict!);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// MySQL Connection
// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Clave_Por_Defecto_Muy_Larga_1234567890!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Iniciando en modo: {(builder.Environment.IsDevelopment() ? "Desarrollo (AWS)" : "Producción (Localhost)")}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// File Storage Service
builder.Services.AddHttpContextAccessor();

var useS3 = builder.Configuration.GetValue<bool>("FileStorage:UseS3");
if (useS3)
{
    // Configurar AWS SDK
    var awsOptions = builder.Configuration.GetAWSOptions();
    awsOptions.Region = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["FileStorage:S3:Region"]);
    awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(
        builder.Configuration["FileStorage:S3:AccessKey"], 
        builder.Configuration["FileStorage:S3:SecretKey"]);
    
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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Dashboard Premium para probar la API
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseMiddleware<UserStatusMiddleware>(); // Validación de estado en tiempo real
app.UseAuthorization();
app.MapControllers();

// La base de datos se maneja manualmente via scripts SQL

app.Run();
