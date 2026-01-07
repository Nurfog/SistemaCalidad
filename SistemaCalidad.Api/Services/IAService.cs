using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaCalidad.Api.Services;

public interface IIAService
{
    Task<string> GenerarRespuesta(string pregunta, string contextoDocumento, string usuario = "sistema", string? sessionId = null);
    Task<string> SincronizarS3Async();
}

public class IAService : IIAService
{
    private readonly HttpClient _httpClient;
    private readonly string _aiBaseUrl;
    private readonly IConfiguration _configuration;

    public IAService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _aiBaseUrl = Environment.GetEnvironmentVariable("AI_API_URL") 
                     ?? configuration["AI_API_URL"] 
                     ?? "http://localhost:8000";
    }

    public async Task<string> SincronizarS3Async()
    {
        var s3Config = new
        {
            aws_access_key_id = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY") ?? _configuration["FileStorage:S3:AccessKey"],
            aws_secret_access_key = Environment.GetEnvironmentVariable("AWS_SECRET_KEY") ?? _configuration["FileStorage:S3:SecretKey"],
            aws_region = Environment.GetEnvironmentVariable("AWS_REGION") ?? _configuration["FileStorage:S3:Region"] ?? "us-east-2",
            bucket_name = Environment.GetEnvironmentVariable("AWS_S3_BUCKET") ?? _configuration["FileStorage:S3:BucketName"]
        };

        var json = JsonSerializer.Serialize(s3Config);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_aiBaseUrl.TrimEnd('/')}/s3/sync", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error sincronizando S3 con IA: {response.StatusCode} - {result}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IAService] Error en sincronización S3: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GenerarRespuesta(string pregunta, string contextoDocumento, string usuario = "sistema", string? sessionId = null)
    {
        // 1. Asegurar que el usuario de servicio existe en OpenCCB
        // Usamos un usuario de servicio centralizado para evitar problemas de contraseñas de usuarios humanos
        var serviceUser = Environment.GetEnvironmentVariable("AI_SERVICE_USER") ?? "sgc_sistema";
        await AsegurarAutenticacion(serviceUser);

        var prompt = $@"
Eres un experto en Gestión de Calidad para el Instituto Chileno Norteamericano (Norma NCh 2728).
Tienes acceso al siguiente contenido de un documento oficial del sistema:

--- INICIO DOCUMENTO ---
{contextoDocumento}
--- FIN DOCUMENTO ---

Responde a la siguiente pregunta del usuario basándote ÚNICAMENTE en la información del documento anterior. 
Si la respuesta no está en el documento, indica claramente que no se menciona.
Se conciso, profesional y cita secciones si es posible.

Pregunta: {pregunta}";

        var requestBody = new
        {
            username = serviceUser,
            prompt = prompt,
            session_id = sessionId,
            use_kb = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsync($"{_aiBaseUrl.TrimEnd('/')}/chat", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en OpenCCB AI API: {response.StatusCode} - {error}");
            }

            Console.WriteLine($"[IAService] Headers recibidos en {sw.Elapsed.TotalSeconds:F2}s. Leyendo contenido...");
            var respuesta = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[IAService] Lectura completa en {sw.Elapsed.TotalSeconds:F2}s.");
            
            return respuesta;
        }
        catch (Exception ex)
        {
            var msg = $"Error al conversar con la IA en {_aiBaseUrl.TrimEnd('/')}/chat: {ex.Message}";
            Console.WriteLine($"[IAService] {msg}");
            throw new Exception(msg);
        }
    }

    private async Task AsegurarAutenticacion(string usuario)
    {
        try
        {
            var pass = Environment.GetEnvironmentVariable("AI_SERVICE_PASS") ?? "sgc_sistema_pass_2026!";
            var regBody = new { username = usuario, password = pass };
            var regJson = JsonSerializer.Serialize(regBody);
            var regContent = new StringContent(regJson, Encoding.UTF8, "application/json");
            
            // Intento de Registro (si falla porque ya existe, lo ignoramos)
            await _httpClient.PostAsync($"{_aiBaseUrl.TrimEnd('/')}/register", regContent);
            
            // Intento de Login
            await _httpClient.PostAsync($"{_aiBaseUrl.TrimEnd('/')}/login", regContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IAService] Error de autenticación de servicio para {usuario}: {ex.Message}");
        }
    }
}
