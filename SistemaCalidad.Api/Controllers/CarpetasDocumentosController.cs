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
    
    // GET: api/CarpetasDocumentos/Arbol (Traer todo el árbol)
    [HttpGet("Arbol")]
    public async Task<ActionResult<IEnumerable<object>>> GetArbolCarpetas() 
    {
        try 
        {
            var todasLasCarpetas = await _context.CarpetasDocumentos
                .OrderBy(c => c.Nombre)
                .AsNoTracking() // Mejora rendimiento para lecturas
                .ToListAsync();

            // Construir árbol desde las raíces
            var raices = todasLasCarpetas.Where(c => c.ParentId == null).ToList();
            
            var tree = raices.Select(c => MapToTree(c, todasLasCarpetas, 0)).ToList();
            
            return Ok(tree);
        }
        catch (Exception ex)
        {
            // Logging preventivo para diagnósticos futuros
            System.Console.WriteLine($"Error crítico en Arbol: {ex.Message}");
            return StatusCode(500, "Error interno al procesar la jerarquía");
        }
    }

    private object MapToTree(CarpetaDocumento c, List<CarpetaDocumento> todas, int depth)
    {
        // Protección contra recursión infinita (StackOverflow)
        if (depth > 20) return new { c.Id, c.Nombre, c.Color, subCarpetas = new List<object>(), Error = "Exceso de profundidad detectado" };

        return new
        {
            c.Id,
            c.Nombre,
            c.Color,
            subCarpetas = todas.Where(x => x.ParentId == c.Id)
                               .Select(x => MapToTree(x, todas, depth + 1))
                               .ToList()
        };
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
        // Evitar ciclos (que una carpeta sea su propio hijo o moverla dentro de su descendencia)
        if (carpeta.ParentId != existing.ParentId)
        {
            if (carpeta.ParentId == id) return BadRequest("Una carpeta no puede ser su propio padre.");
            
            if (carpeta.ParentId.HasValue)
            {
                // Verificar si el nuevo padre es un descendiente de la carpeta actual
                if (await EsDescendiente(id, carpeta.ParentId.Value))
                {
                    return BadRequest("No se puede mover una carpeta dentro de una de sus propias subcarpetas.");
                }
            }
            
            existing.ParentId = carpeta.ParentId;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> EsDescendiente(int rootId, int targetId)
    {
        var actual = await _context.CarpetasDocumentos.FindAsync(targetId);
        while (actual != null && actual.ParentId.HasValue)
        {
            if (actual.ParentId == rootId) return true;
            actual = await _context.CarpetasDocumentos.FindAsync(actual.ParentId);
        }
        return false;
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
        // Chequeamos si hay documentos asociados a esta carpeta
        var tieneDocumentos = await _context.Documentos.AnyAsync(d => d.Carpetas.Any(c => c.Id == id));
        
        if (carpeta.Subcarpetas.Any() || tieneDocumentos)
        {
            return BadRequest("No se puede eliminar la carpeta porque no está vacía (tiene subcarpetas o documentos).");
        }

        _context.CarpetasDocumentos.Remove(carpeta);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
