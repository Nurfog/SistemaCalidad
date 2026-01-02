using System.IdentityModel.Tokens.Jwt;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Services;

public interface IAuditoriaService
{
    Task RegistrarAccionAsync(string accion, string entidad, int entidadId, string? detalle = null);
}

public class AuditoriaService : IAuditoriaService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditoriaService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RegistrarAccionAsync(string accion, string entidad, int entidadId, string? detalle = null)
    {
        var usuario = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Sistema";
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        var log = new Models.AuditoriaAcceso
        {
            Usuario = usuario,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle,
            IpOrigen = ip,
            Fecha = DateTime.UtcNow
        };

        _context.AuditoriaAccesos.Add(log);
        await _context.SaveChangesAsync();
    }
}
