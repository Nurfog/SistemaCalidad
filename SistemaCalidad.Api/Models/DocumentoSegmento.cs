using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

public class DocumentoSegmento
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentoId { get; set; }

    [ForeignKey("DocumentoId")]
    public Documento? Documento { get; set; }

    [Required]
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Vector de embeddings almacenado como BLOB binario (float[] convertido a bytes)
    /// </summary>
    [Required]
    [Column(TypeName = "longblob")]
    public byte[] Vector { get; set; } = Array.Empty<byte>();

    public int Pagina { get; set; }

    public string? Seccion { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Propiedad calculada para facilitar el uso en C#
    [NotMapped]
    public float[]? EmbeddingArray 
    {
        get 
        {
            if (Vector == null || Vector.Length == 0) return null;
            var floats = new float[Vector.Length / 4];
            Buffer.BlockCopy(Vector, 0, floats, 0, Vector.Length);
            return floats;
        }
        set 
        {
            if (value == null) { Vector = Array.Empty<byte>(); return; }
            var bytes = new byte[value.Length * 4];
            Buffer.BlockCopy(value, 0, bytes, 0, bytes.Length);
            Vector = bytes;
        }
    }
}
