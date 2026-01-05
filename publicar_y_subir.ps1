# SCRIPT DE PUBLICACION AUTOMATIZADA API SISTEMA CALIDAD (MODO FTP)
# Requisitos: Tener instalado el SDK de .NET 9

Write-Host "----------------------------------------------------" -ForegroundColor Cyan
Write-Host "🚀 Iniciando proceso de publicación de la API..."
Write-Host "----------------------------------------------------" -ForegroundColor Cyan

$proyectoDir = "d:\mio\DEV\SistemaCalidad\SistemaCalidad.Api"
$publicacionDir = "d:\mio\DEV\SistemaCalidad\publish"

# --- CONFIGURACION FIJA DEL SERVIDOR ---
$ftpServerBase = "ftp://norteamericano.com/SistemaCalidad"
$ftpUser       = "desarrollo"
# ---------------------------------------

# 1. Limpiar y Publicar
if (Test-Path $publicacionDir) { 
    Write-Host "🧹 Limpiando carpeta de publicación anterior..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $publicacionDir 
}

Write-Host "📦 Compilando y publicando en modo Release..." -ForegroundColor Yellow
Set-Location $proyectoDir
dotnet publish -c Release -o $publicacionDir /p:PublishReadyToRun=false /p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en la compilación. Abortando." -ForegroundColor Red
    exit
}

Write-Host "✅ Publicación local generada correctamente." -ForegroundColor Green

# 2. Copiar el archivo .env a la carpeta de publicación
if (Test-Path "$proyectoDir\.env") {
    Write-Host "🔐 Incluyendo archivo .env en el paquete de subida..." -ForegroundColor Cyan
    Copy-Item "$proyectoDir\.env" -Destination "$publicacionDir\.env" -Force
}

# 3. Pedir Contraseña
Write-Host "`n----------------------------------------------------"
Write-Host "🌐 Autenticando para: $ftpServerBase"
Write-Host "👤 Usuario: $ftpUser"
Write-Host "----------------------------------------------------"
$ftpPass = Read-Host "Ingrese Contrasena para el usuario $ftpUser" -AsSecureString

# Convertir password
$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($ftpPass)
$plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr)

# 4. Subir archivos
Write-Host "`n🚚 Iniciando transferencia de archivos (Modo Pasivo)..." -ForegroundColor Yellow

$archivos = Get-ChildItem -Path $publicacionDir -Recurse | Where-Object { ! $_.PSIsContainer }
$total = $archivos.Count
$actual = 0

foreach ($archivo in $archivos) {
    $actual++
    $nombreRelativo = $archivo.FullName.Substring($publicacionDir.Length + 1).Replace("\", "/")
    $urlDestino = ($ftpServerBase.TrimEnd('/') + "/" + $nombreRelativo)
    
    Write-Host "[$actual/$total] 📤 Enviando: $nombreRelativo ..." -ForegroundColor Gray
    try {
        $uri = [System.Uri]$urlDestino
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.UsePassive = $true  # <--- Crucial para evitar errores de conexión
        $request.UseBinary = $true
        $request.KeepAlive = $false

        $fileBytes = [System.IO.File]::ReadAllBytes($archivo.FullName)
        $request.ContentLength = $fileBytes.Length
        
        $requestStream = $request.GetRequestStream()
        $requestStream.Write($fileBytes, 0, $fileBytes.Length)
        $requestStream.Close()
        $requestStream.Dispose()
        
        $response = $request.GetResponse()
        $response.Close()
        $response.Dispose()
    } catch {
        Write-Host "❌ Error en $nombreRelativo : $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n----------------------------------------------------"
Write-Host "🎉 ¡DESPLIEGUE FINALIZADO EN EL SERVIDOR!" -ForegroundColor Green
Write-Host "URL Base: $ftpServerBase"
Write-Host "----------------------------------------------------"
