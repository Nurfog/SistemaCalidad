using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaCalidad.Api.Services;

public interface IIAService
{
    Task<string> GenerarRespuesta(string pregunta, string contextoDocumento);
}

public class IAService : IIAService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ModelUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    public IAService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new ArgumentNullException("GoogleAI:ApiKey no está configurada.");
    }

    public async Task<string> GenerarRespuesta(string pregunta, string contextoDocumento)
    {
        var prompt = $@"
Eres un experto en Gestión de Calidad (Norma NCh 2728).
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
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{ModelUrl}?key={_apiKey}", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error en Google AI API: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        
        // Navegar la respuesta de Gemini: candidates[0].content.parts[0].text
        try
        {
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "No se generó respuesta.";
        }
        catch
        {
            return "Error al procesar la respuesta de la IA.";
        }
    }
}
