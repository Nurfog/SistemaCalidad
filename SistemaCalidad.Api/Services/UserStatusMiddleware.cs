using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SistemaCalidad.Api.Data;

namespace SistemaCalidad.Api.Services;

public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public UserStatusMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
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
                // CACHE-ASIDE PATTERN
                string cacheKey = $"user_status_{username}";
                
                // Si ya validamos hace poco (5 min), pasamos (asumimos activo)
                if (!_cache.TryGetValue(cacheKey, out bool estaActivo))
                {
                    // Si NO está en caché, consultamos a la BD
                    try
                    {
                        var conn = dbContext.Database.GetDbConnection();
                        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

                        using (var cmd = conn.CreateCommand())
                        {
                            // Usamos sige_sam_v3 explícitamente.
                            cmd.CommandText = "SELECT Activo FROM sige_sam_v3.usuario WHERE idUsuario = @u LIMIT 1";
                            var p1 = cmd.CreateParameter();
                            p1.ParameterName = "@u";
                            p1.Value = username;
                            cmd.Parameters.Add(p1);

                            var result = await cmd.ExecuteScalarAsync();

                            estaActivo = true; // Por defecto activo si no se encuentra (fail open seguro local)
                            if (result != null && result != DBNull.Value)
                            {
                                int val = Convert.ToInt32(result);
                                if (val != 1) estaActivo = false;
                            }
                        }

                        // Guardar en caché por 5 minutos
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                        _cache.Set(cacheKey, estaActivo, cacheEntryOptions);
                    }
                    catch (Exception ex)
                    {
                        // Fallo silencioso (Fail Open)
                        Console.WriteLine($"[UserStatusMiddleware] ERROR CRÍTICO DB: {ex.Message}");
                        estaActivo = true; // Dejar pasar en caso de error técnico
                    }
                }

                if (!estaActivo)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { mensaje = "Tu cuenta ha sido desactivada en el sistema central." });
                    return; // Bloquear petición
                }
            }
        }

        await _next(context);
    }
}
