using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;

namespace SistemaCalidad.Api.Services;

public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    public UserStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        // Solo verificamos si el usuario ya está autenticado via JWT
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var username = context.User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
            {
                // Consultamos directamente el estado en el origen
                var user = await dbContext.UsuariosExternos
                    .AsNoTracking() // Optimización: Solo lectura rápida
                    .FirstOrDefaultAsync(u => u.usuario == username);

                // Si el usuario no existe o activo != 1, bloqueamos la petición inmediatamente
                if (user == null || user.activo != 1)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { mensaje = "Tu cuenta ha sido desactivada en el sistema central." });
                    return;
                }
            }
        }

        await _next(context);
    }
}
