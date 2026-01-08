using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SistemaCalidad.Api.Services;

public class IASyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IASyncBackgroundService> _logger;

    public IASyncBackgroundService(IServiceProvider serviceProvider, ILogger<IASyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servidor de Sincronización Programada IA iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Ejecutar cada 60 días (aprox. 2 meses)
            // Para pruebas iniciales podrías reducirlo, pero cumpliremos el requisito: 60 días.
            // TimeSpan.FromDays(60)
            
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var iaService = scope.ServiceProvider.GetRequiredService<IIAService>();
                    _logger.LogInformation("Iniciando Sincronización Bimestral Automática de la IA...");
                    
                    await iaService.SincronizarS3Async();
                    
                    _logger.LogInformation("Sincronización Bimestral completada exitosamente.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización automática de la IA.");
            }

            // Esperar 60 días antes de la siguiente ejecución
            await Task.Delay(TimeSpan.FromDays(60), stoppingToken);
        }
    }
}
