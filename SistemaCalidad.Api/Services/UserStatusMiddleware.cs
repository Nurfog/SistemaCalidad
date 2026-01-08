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
        // 1. Filtrar endpoints: Solo API y NO SignalR (para evitar saturar DB en sockets)
        if (!context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/api/hub"))
        {
            await _next(context);
            return;
        }

        // 2. Verificar autenticación
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var username = context.User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
            {
                try
                {
                    // 3. Consulta ADO.NET Directa (Bylass EF Mapping issues)
                    // Usamos sige_sam_v3 explícitamente.
                    var conn = dbContext.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Activo FROM sige_sam_v3.usuario WHERE idUsuario = @u LIMIT 1";
                        var p1 = cmd.CreateParameter();
                        p1.ParameterName = "@u";
                        p1.Value = username;
                        cmd.Parameters.Add(p1);

                        var result = await cmd.ExecuteScalarAsync();

                        // Si encontramos el usuario
                        if (result != null && result != DBNull.Value)
                        {
                            int activo = Convert.ToInt32(result);
                            if (activo != 1)
                            {
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                await context.Response.WriteAsJsonAsync(new { mensaje = "Tu cuenta ha sido desactivada en el sistema central." });
                                return; // Bloquear petición
                            }
                        }
                        // Si no se encuentra el usuario, por seguridad podríamos bloquear o dejar pasar.
                        // Asumiremos que si tiene Token válido pero no está en la tabla externa (quizás es admin local?), lo dejamos pasar por ahora.
                    }
                }
                catch (Exception ex)
                {
                    // Fallo silencioso en producción para no tumbar la API (Fail Open)
                    // Pero logueamos el error en consola severa
                    Console.WriteLine($"[UserStatusMiddleware] ERROR CRÍTICO al validar usuario {username}: {ex.Message}");
                    // Opcional: Bloquear si se prefiere seguridad estricta
                }
            }
        }

        await _next(context);
    }
}
