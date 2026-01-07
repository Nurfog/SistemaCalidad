using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaCalidad.Api.Services;

public interface IIAService
{
    Task<string> GenerarRespuesta(string pregunta, string contextoDocumento, string usuario = "sistema", string? sessionId = null);
}

public class IAService : IIAService
{
    private readonly HttpClient _httpClient;
    private readonly string _aiBaseUrl;

    public IAService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _aiBaseUrl = configuration["AI_API_URL"] ?? "http://localhost:8000";
    }

    public async Task<string> GenerarRespuesta(string pregunta, string contextoDocumento, string usuario = "sistema", string? sessionId = null)
    {
        // 1. Asegurar que el usuario existe en OpenCCB
        await AsegurarAutenticacion(usuario);

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
            username = usuario,
            prompt = prompt,
            session_id = sessionId,
            use_kb = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_aiBaseUrl.TrimEnd('/')}/chat", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en OpenCCB AI API: {response.StatusCode} - {error}");
            }

            return await response.Content.ReadAsStringAsync();
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
        // Nota: OpenCCB AI parece usar un flujo simple de usuario.
        // Intentamos registrar al usuario por si no existe (la API suele ignorar si ya existe o devuelve error controlado)
        try
        {
            var regBody = new { username = usuario, password = "default_password_sgc" };
            var regJson = JsonSerializer.Serialize(regBody);
            var regContent = new StringContent(regJson, Encoding.UTF8, "application/json");
            
            // Intento de Registro (si falla porque ya existe, lo ignoramos y seguimos)
            await _httpClient.PostAsync($"{_aiBaseUrl.TrimEnd('/')}/register", regContent);
            
            // Intento de Login
            await _httpClient.PostAsync($"{_aiBaseUrl.TrimEnd('/')}/login", regContent);
        }
        catch (Exception ex)
        {
            // Logging silencioso, si el servicio no requiere login estricto para /chat seguiremos adelante
            Console.WriteLine($"[IAService] Intento de autenticación para {usuario}: {ex.Message}");
        }
    }
}
