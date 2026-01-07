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
            use_kb = true // Activar RAG local si OpenCCB lo soporta por defecto
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

            // Las respuestas de OpenCCB pueden venir como un stream de texto simple o JSON al final
            var resultText = await response.Content.ReadAsStringAsync();
            
            // Si la respuesta contiene un JSON al final con el session_id, intentamos limpiarlo para el usuario
            // aunque usualmente el usuario solo quiere el texto.
            return resultText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IAService] Error conectando con la IA Local: {ex.Message}");
            throw new Exception($"No se pudo conectar con el servicio de IA local en {_aiBaseUrl}. Asegúrate de que Ollama y la API de OpenCCB estén corriendo.");
        }
    }
}
