using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;
using SistemaCalidad.Api.Services;

namespace SistemaCalidad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileService;

    public DocumentsController(ApplicationDbContext context, IFileStorageService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    // NCh 2728 4.2.3: Listar documentos controlados
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
    {
        return await _context.Documents.Include(d => d.Revisions).ToListAsync();
    }

    // NCh 2728 4.2.3.a: Crear y aprobar documentos
    [HttpPost]
    public async Task<ActionResult<Document>> CreateDocument([FromForm] string title, [FromForm] string code, [FromForm] DocumentType type, [FromForm] ProcessArea area, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File is required.");

        var document = new Document
        {
            Title = title,
            Code = code,
            Type = type,
            Area = area,
            Status = DocumentStatus.Draft,
            CurrentVersion = 1
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var filePath = await _fileService.SaveFileAsync(file.OpenReadStream(), file.FileName, "Documents");

        var revision = new DocumentVersion
        {
            DocumentId = document.Id,
            VersionNumber = 1,
            ChangeDescription = "Initial creation",
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            IsCurrent = true,
            CreatedBy = "System Admin" // Change to actual user in production
        };

        _context.DocumentVersions.Add(revision);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDocuments), new { id = document.Id }, document);
    }

    // NCh 2728 4.2.3.b/c: Revisar y actualizar documentos
    [HttpPost("{id}/revision")]
    public async Task<IActionResult> AddRevision(int id, [FromForm] string changeDescription, IFormFile file)
    {
        var document = await _context.Documents.Include(d => d.Revisions).FirstOrDefaultAsync(d => d.Id == id);
        if (document == null) return NotFound();

        // Mark previous current as not current
        var current = document.Revisions.FirstOrDefault(r => r.IsCurrent);
        if (current != null) current.IsCurrent = false;

        var filePath = await _fileService.SaveFileAsync(file.OpenReadStream(), file.FileName, "Documents");
        
        document.CurrentVersion++;
        document.UpdatedAt = DateTime.UtcNow;

        var revision = new DocumentVersion
        {
            DocumentId = document.Id,
            VersionNumber = document.CurrentVersion,
            ChangeDescription = changeDescription,
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            IsCurrent = true,
            CreatedBy = "System Admin"
        };

        _context.DocumentVersions.Add(revision);
        await _context.SaveChangesAsync();

        return Ok(document);
    }

    // Descargar el archivo vigente
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var document = await _context.Documents.Include(d => d.Revisions).FirstOrDefaultAsync(d => d.Id == id);
        if (document == null) return NotFound();

        var currentVersion = document.Revisions.FirstOrDefault(r => r.IsCurrent);
        if (currentVersion == null) return NotFound("No active version found.");

        var fileData = await _fileService.GetFileAsync(currentVersion.FilePath);
        return File(fileData.Content, fileData.ContentType, currentVersion.FileName);
    }
}
