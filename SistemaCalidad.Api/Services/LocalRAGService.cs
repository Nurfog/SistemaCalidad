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

        // 1. Limpiar segmentos previos
        var antiguos = _context.DocumentoSegmentos.Where(s => s.DocumentoId == documentoId);
        _context.DocumentoSegmentos.RemoveRange(antiguos);

        // 2. Fragmentación (Chunking) básica por párrafos
        // En una implementación avanzada usaríamos solapamiento (overlap)
        var párrafos = textoCompleto.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(p => p.Length > 50) // Ignorar fragmentos muy cortos
                        .ToList();

        foreach (var párrafo in párrafos)
        {
            // Generar Embedding (Localmente en CPU)
            var embedding = _embedder.Embed(párrafo);
            
            var segmento = new DocumentoSegmento
            {
                DocumentoId = documentoId,
                Contenido = párrafo,
                EmbeddingArray = embedding.Values.ToArray()
            };

            await _context.DocumentoSegmentos.AddAsync(segmento);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Indexación completada. {párrafos.Count} segmentos creados.");
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
