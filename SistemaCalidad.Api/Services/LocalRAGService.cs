using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;
using SmartComponents.LocalEmbeddings;

namespace SistemaCalidad.Api.Services;

public interface ILocalRAGService
{
    Task IndexarDocumentoAsync(int documentoId, string textoCompleto);
    Task<List<DocumentoSegmento>> BuscarSimilares(string consulta, int limite = 5);
}

public class LocalRAGService : ILocalRAGService, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LocalEmbedder _embedder;
    private readonly ILogger<LocalRAGService> _logger;

    public LocalRAGService(ApplicationDbContext context, LocalEmbedder embedder, ILogger<LocalRAGService> logger)
    {
        _context = context;
        _embedder = embedder;
        _logger = logger;
    }

    public async Task IndexarDocumentoAsync(int documentoId, string textoCompleto)
    {
        _logger.LogInformation($"Indexando documento {documentoId} para RAG Local...");

        if (string.IsNullOrWhiteSpace(textoCompleto)) 
        {
            Console.WriteLine($"[RAG] OMITIDO: Documento {documentoId} tiene texto vacío.");
            return;
        }

        // 2. Fragmentación (Chunking) básica por párrafos
        var párrafos = textoCompleto.Split(new[] { "\n\n", "\r\n\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p) && p.Length > 20) 
                        .ToList();

        Console.WriteLine($"[RAG] Documento {documentoId}: Texto {textoCompleto.Length} chars -> {párrafos.Count} párrafos.");

        if (párrafos.Count == 0)
        {
             Console.WriteLine($"[RAG] OMITIDO: Documento {documentoId} no generó párrafos válidos tras limpieza.");
             return;
        }

        var nuevosSegmentos = new List<DocumentoSegmento>();
        
        foreach (var párrafoRaw in párrafos)
        {
            try 
            {
                // Limitar longitud
                var párrafo = párrafoRaw.Length > 2000 ? párrafoRaw.Substring(0, 2000) : párrafoRaw;
                if (string.IsNullOrWhiteSpace(párrafo)) continue;

                var embedding = _embedder.Embed(párrafo);
                
                // Validación paranoia: vector vacío?
                if (embedding.Values.Length == 0)
                {
                     Console.WriteLine($"[RAG] ERROR: Embedding vacío para doc {documentoId}.");
                     continue;
                }

                nuevosSegmentos.Add(new DocumentoSegmento
                {
                    DocumentoId = documentoId,
                    Contenido = párrafo,
                    // Asegurar copia del array para evitar problemas de referencia
                    EmbeddingArray = embedding.Values.ToArray() 
                });
            }
            catch (Exception ex)
            {
                // [IMPORTANTE] LOG DE ERROR REAL
                Console.WriteLine($"[RAG] EXCEPCIÓN Embedding Doc {documentoId}: {ex.Message}");
                // _logger.LogError ...
            }
        }

        Console.WriteLine($"[RAG] Documento {documentoId}: Generados {nuevosSegmentos.Count} segmentos. (Intentando guardar...)");

        // SOLO guardamos si se generó algo. Si falló todo, mantenemos lo viejo.
        if (nuevosSegmentos.Count > 0)
        {
            // 1. Limpiar previos
            var antiguos = _context.DocumentoSegmentos.Where(s => s.DocumentoId == documentoId);
            _context.DocumentoSegmentos.RemoveRange(antiguos);

            // 2. Insertar nuevos
            await _context.DocumentoSegmentos.AddRangeAsync(nuevosSegmentos);
            
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[RAG] EXITOSO Documento {documentoId} guardado.");
        }
        else
        {
            Console.WriteLine($"[RAG] FALLIDO Documento {documentoId}: 0 segmentos válidos generados.");
        }
        
        if (nuevosSegmentos.Count > 50) GC.Collect();
    }

    public async Task<List<DocumentoSegmento>> BuscarSimilares(string consulta, int limite = 5)
    {
        // 1. Convertir consulta a vector
        var queryEmbedding = _embedder.Embed(consulta);

        // 2. Traer todos los segmentos (Optimizado: en producción usaríamos filtros o caché)
        // Como son pocos archivos, cargar los vectores en memoria es ultra-rápido
        var segmentos = await _context.DocumentoSegmentos.ToListAsync();

        // 3. Calcular similitud en memoria
        var resultados = segmentos
            .Select(s => new { Segmento = s, Similitud = Similarity(queryEmbedding.Values.ToArray(), s.EmbeddingArray) })
            .Where(r => r.Similitud > 0.5) // Umbral de relevancia
            .OrderByDescending(r => r.Similitud)
            .Take(limite)
            .Select(r => r.Segmento)
            .ToList();

        return resultados;
    }

    // Cálculo manual de Similitud de Coseno (Para MySQL 8.0 local)
    private float Similarity(float[]? v1, float[]? v2)
    {
        if (v1 == null || v2 == null || v1.Length != v2.Length) return 0;
        
        float dotProduct = 0;
        float normA = 0;
        float normB = 0;
        
        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            normA += v1[i] * v1[i];
            normB += v2[i] * v2[i];
        }
        
        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    public void Dispose()
    {
        _embedder?.Dispose();
    }
}
