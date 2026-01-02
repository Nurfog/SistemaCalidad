using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;
using SistemaCalidad.Api.Services;

namespace SistemaCalidad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileService;

    public RecordsController(ApplicationDbContext context, IFileStorageService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QualityRecord>>> GetRecords()
    {
        return await _context.QualityRecords.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<QualityRecord>> CreateRecord([FromForm] string name, [FromForm] string identifier, [FromForm] int retentionYears, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File is required for evidence.");

        var filePath = await _fileService.SaveFileAsync(file.OpenReadStream(), file.FileName, "Records");

        var record = new QualityRecord
        {
            Name = name,
            Identifier = identifier,
            RetentionYears = retentionYears,
            FilePath = filePath,
            StorageDate = DateTime.UtcNow
        };

        _context.QualityRecords.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRecords), new { id = record.Id }, record);
    }
}
