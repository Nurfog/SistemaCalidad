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

# 2. Datos de Conexión FTP
Write-Host "`n----------------------------------------------------"
Write-Host "🌐 Configuración de Servidor FTP (Windows Server)"
Write-Host "----------------------------------------------------"
$ftpServer = Read-Host "Ingrese la URL o IP del servidor FTP (ej: ftp://12.34.56.78)"
$ftpUser = Read-Host "Ingrese el Usuario FTP"
$ftpPass = Read-Host "Ingrese la Contraseña FTP" -AsSecureString

# Convertir password a texto plano para el objeto WebClient
$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($ftpPass)
$plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr)

# 3. Subir archivos
Write-Host "`n🚚 Subiendo archivos al servidor..." -ForegroundColor Yellow

$webClient = New-Object System.Net.WebClient
$webClient.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)

$archivos = Get-ChildItem -Path $publicacionDir -Recurse | Where-Object { ! $_.PSIsContainer }

foreach ($archivo in $archivos) {
    # Calcular ruta relativa para el FTP
    $rutaRelativa = $archivo.FullName.Replace($publicacionDir, "").Replace("\", "/")
    $ftpDestino = "$ftpServer$rutaRelativa"
    
    Write-Host "📤 Subiendo: $rutaRelativa ..." -ForegroundColor Gray
    try {
        $uri = New-Object System.Uri($ftpDestino)
        $webClient.UploadFile($uri, $archivo.FullName)
    } catch {
        Write-Host "⚠ Error subiendo $rutaRelativa : $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n----------------------------------------------------"
Write-Host "🎉 ¡PROCESO FINALIZADO!" -ForegroundColor Green
Write-Host "Los archivos han sido cargados. Recuerde configurar el sitio en IIS en Windows Server."
Write-Host "----------------------------------------------------"
