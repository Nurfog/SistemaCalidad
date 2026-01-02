# SCRIPT DE PUBLICACIÓN AUTOMATIZADA API SISTEMA CALIDAD
# Requisitos: Tener instalado el SDK de .NET 9

Write-Host "----------------------------------------------------" -ForegroundColor Cyan
Write-Host "🚀 Iniciando proceso de publicación de la API..."
Write-Host "----------------------------------------------------" -ForegroundColor Cyan

$proyectoDir = "d:\mio\DEV\SistemaCalidad\SistemaCalidad.Api"
$publicacionDir = "d:\mio\DEV\SistemaCalidad\publish"

# 1. Limpiar y Publicar
if (Test-Path $publicacionDir) { Remove-Item -Recurse -Force $publicacionDir }
Write-Host "📦 Compilando y publicando en modo Release..." -ForegroundColor Yellow

Set-Location $proyectoDir
dotnet publish -c Release -o $publicacionDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en la compilación. Abortando." -ForegroundColor Red
    exit
}

Write-Host "✅ Publicación local generada en $publicacionDir" -ForegroundColor Green

# --- NUEVO: Copiar el archivo .env a la carpeta de publicación ---
if (Test-Path "$proyectoDir\.env") {
    Write-Host "🔐 Incluyendo archivo .env en la publicación..." -ForegroundColor Cyan
    Copy-Item "$proyectoDir\.env" -Destination "$publicacionDir\.env" -Force
}
# -----------------------------------------------------------------

# 2. Datos de Conexion FTP
Write-Host "`n----------------------------------------------------"
Write-Host "🌐 Configuracion de Servidor FTP (Windows Server)"
Write-Host "----------------------------------------------------"
$ftpHost = Read-Host "Ingrese la Ip o Host (ej: calidad.norteamericano.cl)"
$ftpUser = Read-Host "Ingrese el Usuario FTP"
$ftpPass = Read-Host "Ingrese la Contrasena FTP" -AsSecureString

# Asegurar que la URL tenga el formato ftp://
if (-not $ftpHost.StartsWith("ftp://")) { $ftpServer = "ftp://$ftpHost" } else { $ftpServer = $ftpHost }

# Convertir password
$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($ftpPass)
$plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr)

# 3. Subir archivos
Write-Host "`n🚚 Subiendo archivos al servidor..." -ForegroundColor Yellow

$webClient = New-Object System.Net.WebClient
$webClient.Proxy = $null # Evita problemas de lentitud con proxys
$webClient.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)

$archivos = Get-ChildItem -Path $publicacionDir -Recurse | Where-Object { ! $_.PSIsContainer }

foreach ($archivo in $archivos) {
    $rutaRelativa = $archivo.FullName.Replace($physicalPath_local, "").Replace($publicacionDir, "").Replace("\", "/")
    $ftpDestino = "$ftpServer/$rutaRelativa".Replace("//", "/")
    
    Write-Host "📤 Enviando: $rutaRelativa ..." -ForegroundColor Gray
    try {
        $uri = New-Object System.Uri($ftpDestino)
        $webClient.UploadFile($uri, "STOR", $archivo.FullName)
    } catch {
        Write-Host "?? Error en $rutaRelativa : $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n----------------------------------------------------"
Write-Host "🎉 ¡PROCESO FINALIZADO!" -ForegroundColor Green
Write-Host "Los archivos han sido cargados. Recuerde configurar el sitio en IIS en Windows Server."
Write-Host "----------------------------------------------------"
