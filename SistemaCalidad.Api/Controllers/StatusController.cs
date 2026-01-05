using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Amazon.S3;
using Microsoft.AspNetCore.Authorization;

namespace SistemaCalidad.Api.Controllers;

[ApiController]
public class StatusController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IAmazonS3? _s3Client;
    private readonly IConfiguration _configuration;

    public StatusController(HealthCheckService healthCheckService, IConfiguration configuration, IAmazonS3? s3Client = null)
    {
        _healthCheckService = healthCheckService;
        _s3Client = s3Client;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("api/status/health")]
    public async Task<IActionResult> GetHealth()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        // Custom S3 Check
        var s3Status = "Healthy";
        var useS3 = _configuration.GetValue<bool>("FileStorage:UseS3");
        if (useS3 && _s3Client != null)
        {
            try {
                await _s3Client.ListBucketsAsync();
            } catch {
                s3Status = "Unhealthy";
            }
        }
        else if (useS3) {
            s3Status = "Unhealthy";
        }
        else {
            s3Status = "Not Configured (Using Local Storage)";
        }

        var status = new
        {
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Checks = report.Entries.Select(e => new {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description
            }),
            S3Status = s3Status,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Version = "1.2.0-stable"
        };

        return Ok(status);
    }

    [AllowAnonymous]
    [HttpGet("status")]
    [Produces("text/html")]
    public IActionResult GetStatusPage()
    {
        var html = @"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Estado del Sistema - Calidad NCh 2728</title>
    <link href='https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {
            --bg: #0f172a;
            --card-bg: rgba(30, 41, 59, 0.7);
            --primary: #38bdf8;
            --success: #10b981;
            --danger: #ef4444;
            --text: #f8fafc;
            --text-muted: #94a3b8;
        }

        * { margin: 0; padding: 0; box-sizing: border-box; }

        body {
            font-family: 'Outfit', sans-serif;
            background-color: var(--bg);
            background-image: radial-gradient(circle at top right, #1e293b, #0f172a);
            color: var(--text);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }

        .dashboard {
            width: 100%;
            max-width: 600px;
            background: var(--card-bg);
            backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 24px;
            padding: 40px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
            animation: fadeIn 0.8s ease-out;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }

        header {
            text-align: center;
            margin-bottom: 40px;
        }

        h1 {
            font-size: 2rem;
            font-weight: 600;
            margin-bottom: 8px;
            background: linear-gradient(to right, #38bdf8, #818cf8);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }

        .subtitle {
            color: var(--text-muted);
            font-size: 0.9rem;
            letter-spacing: 1px;
            text-transform: uppercase;
        }

        .status-grid {
            display: grid;
            gap: 20px;
            margin-bottom: 30px;
        }

        .status-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            background: rgba(15, 23, 42, 0.5);
            padding: 16px 24px;
            border-radius: 16px;
            border: 1px solid rgba(255, 255, 255, 0.05);
            transition: transform 0.2s, border-color 0.2s;
        }

        .status-item:hover {
            transform: scale(1.02);
            border-color: rgba(56, 189, 248, 0.3);
        }

        .label {
            font-weight: 400;
            color: var(--text-muted);
        }

        .value {
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .dot {
            width: 10px;
            height: 10px;
            border-radius: 50%;
            background: var(--text-muted);
        }

        .dot.online {
            background: var(--success);
            box-shadow: 0 0 10px var(--success);
            animation: pulse 2s infinite;
        }

        .dot.offline {
            background: var(--danger);
            box-shadow: 0 0 10px var(--danger);
        }

        @keyframes pulse {
            0% { opacity: 1; transform: scale(1); }
            50% { opacity: 0.5; transform: scale(1.2); }
            100% { opacity: 1; transform: scale(1); }
        }

        footer {
            margin-top: 30px;
            text-align: center;
            font-size: 0.8rem;
            color: var(--text-muted);
        }

        .refresh-btn {
            margin-top: 20px;
            background: rgba(56, 189, 248, 0.1);
            color: var(--primary);
            border: 1px solid var(--primary);
            padding: 8px 16px;
            border-radius: 8px;
            cursor: pointer;
            transition: all 0.3s;
            font-family: inherit;
        }

        .refresh-btn:hover {
            background: var(--primary);
            color: var(--bg);
        }
    </style>
</head>
<body>
    <div class='dashboard'>
        <header>
            <h1>SGC Dashboard</h1>
            <p class='subtitle'>Estado de los Servicios en Tiempo Real</p>
        </header>

        <div class='status-grid' id='statusGrid'>
            <div class='status-item'>
                <span class='label'>API Core</span>
                <span class='value'><span id='apiDot' class='dot'></span> <span id='apiText'>Cargando...</span></span>
            </div>
            <div class='status-item'>
                <span class='label'>Base de Datos</span>
                <span class='value'><span id='dbDot' class='dot'></span> <span id='dbText'>Cargando...</span></span>
            </div>
            <div class='status-item'>
                <span class='label'>Almacenamiento S3</span>
                <span class='value'><span id='s3Dot' class='dot'></span> <span id='s3Text'>Cargando...</span></span>
            </div>
        </div>

        <div style='text-align:center'>
            <p id='lastUpdate' style='font-size: 0.8rem; color: var(--text-muted)'></p>
            <button class='refresh-btn' onclick='updateStatus()'>Actualizar Ahora</button>
        </div>

        <footer>
            <p>&copy; 2026 Instituto Chileno Norteamericano | Sistema Calidad NCh 2728</p>
            <p id='versionInfo' style='margin-top:5px'></p>
        </footer>
    </div>

    <script>
        async function updateStatus() {
            try {
                const response = await fetch('/api/status/health');
                const data = await response.json();

                // Actualizar API
                const apiDot = document.getElementById('apiDot');
                apiDot.className = 'dot ' + (data.status === 'Healthy' ? 'online' : 'offline');
                document.getElementById('apiText').innerText = data.status === 'Healthy' ? 'Operativo' : 'Problemas';

                // Actualizar DB
                const dbCheck = data.checks.find(c => c.name === 'Base de Datos');
                const dbDot = document.getElementById('dbDot');
                dbDot.className = 'dot ' + (dbCheck.status === 'Healthy' ? 'online' : 'offline');
                document.getElementById('dbText').innerText = dbCheck.status === 'Healthy' ? 'Conectado' : 'Error';

                // Actualizar S3
                const s3Dot = document.getElementById('s3Dot');
                const isS3Ok = data.s3Status === 'Healthy' || data.s3Status.includes('Local');
                s3Dot.className = 'dot ' + (isS3Ok ? 'online' : 'offline');
                document.getElementById('s3Text').innerText = data.s3Status === 'Healthy' ? 'Amazon S3 Activo' : 
                                                              data.s3Status.includes('Local') ? 'Local Storage' : 'Error de Enlace';

                document.getElementById('lastUpdate').innerText = 'Última actualización: ' + new Date(data.timestamp).toLocaleTimeString();
                document.getElementById('versionInfo').innerText = 'Versión: ' + data.version + ' | Entorno: ' + data.environment;

            } catch (error) {
                console.error('Error al obtener el estado:', error);
            }
        }

        updateStatus();
        setInterval(updateStatus, 30000); // Actualizar cada 30 segundos
    </script>
</body>
</html>
";
        return Content(html, "text/html");
    }
}
