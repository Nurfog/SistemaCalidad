using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CarpetasDocumentosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CarpetasDocumentosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/CarpetasDocumentos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarpetaDocumento>>> GetCarpetas(int? parentId = null)
    {
        // Si parentId es null, trae las carpetas raíz. Si tiene valor, trae sus subcarpetas.
        // Incluimos subcarpetas para saber si tienen hijos (útil para expandir en UI)
        return await _context.CarpetasDocumentos
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.Nombre)
            .Include(c => c.Subcarpetas) 
            .ToListAsync();
    }
    
    // GET: api/CarpetasDocumentos/Arbol (Opcional: Traer todo el árbol)
    [HttpGet("Arbol")]
    public async Task<ActionResult<IEnumerable<object>>> GetArbolCarpetas() 
    {
       // Nota: Implementar recursión cuidadosa si hay muchas carpetas. 
       // Por ahora, el cliente navegará nivel por nivel (Lazy Loading).
       return BadRequest("Use navegación por niveles (parentId)");
    }


    // POST: api/CarpetasDocumentos
    [HttpPost]
    [Authorize(Roles = "Administrador,Escritor")]
    public async Task<ActionResult<CarpetaDocumento>> PostCarpeta(CarpetaDocumento carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta.Nombre))
            return BadRequest("El nombre es requerido");

        carpeta.FechaCreacion = DateTime.UtcNow;
        if (string.IsNullOrEmpty(carpeta.Color)) carpeta.Color = "#fbbf24";
        
        // Validar si el ParentId existe si fue enviado
        if (carpeta.ParentId.HasValue)
        {
            var parentExists = await _context.CarpetasDocumentos.AnyAsync(c => c.Id == carpeta.ParentId);
            if (!parentExists) return BadRequest("La carpeta padre no existe.");
        }

        _context.CarpetasDocumentos.Add(carpeta);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCarpetas), new { id = carpeta.Id }, carpeta);
    }
    
    // PUT: api/CarpetasDocumentos/5 (Renombrar o Mover)
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Escritor")]
    public async Task<IActionResult> PutCarpeta(int id, CarpetaDocumento carpeta)
    {
        if (id != carpeta.Id) return BadRequest();

        var existing = await _context.CarpetasDocumentos.FindAsync(id);
        if (existing == null) return NotFound();
        
        existing.Nombre = carpeta.Nombre;
        existing.Color = carpeta.Color;
        
        // Logica para mover carpeta (cambiar ParentId)
        // Evitar ciclos (que una carpeta sea su propio hijo)
        if (carpeta.ParentId != existing.ParentId)
        {
            if (carpeta.ParentId == id) return BadRequest("Una carpeta no puede ser su propio padre.");
            existing.ParentId = carpeta.ParentId;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/CarpetasDocumentos/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DeleteCarpeta(int id)
    {
        var carpeta = await _context.CarpetasDocumentos
            .Include(c => c.Subcarpetas)
            .Include(c => c.Documentos) // Asumiendo relación en DbContext aunque no explícita en modelo inverso
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (carpeta == null) return NotFound();

        // Verificar si tiene contenido
        // Chequeamos si hay documentos asociados a esta carpeta usando count en la tabla documentos
        var tieneDocumentos = await _context.Documentos.AnyAsync(d => d.CarpetaDocumentoId == id);
        
        if (carpeta.Subcarpetas.Any() || tieneDocumentos)
        {
            return BadRequest("No se puede eliminar la carpeta porque no está vacía (tiene subcarpetas o documentos).");
        }

        _context.CarpetasDocumentos.Remove(carpeta);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
