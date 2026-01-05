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

# 1.1 Compilar Frontend (React)
$frontendDir = "d:\mio\DEV\SistemaCalidad\frontend"
if (Test-Path $frontendDir) {
    Write-Host "⚛️ Compilando Frontend (React)..." -ForegroundColor Magenta
    Set-Location $frontendDir
    # npm install # Descomentar si es la primera vez o hay nuevas dependencias
    npm run build
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Error al compilar el Frontend. Abortando." -ForegroundColor Red
        exit
    }
    
    # 1.2 Copiar Frontend a carpeta personalizada (frontend_app) de la publicación API
    $webRootDir = "$publicacionDir\frontend_app"
    if (!(Test-Path $webRootDir)) { New-Item -ItemType Directory -Force -Path $webRootDir | Out-Null }
    
    Write-Host "📂 Copiando archivos del Frontend a $webRootDir ..." -ForegroundColor Cyan
    Copy-Item "$frontendDir\dist\*" -Destination $webRootDir -Recurse -Force
}

# 2. Copiar el archivo .env a la carpeta de publicación
if (Test-Path "$proyectoDir\.env") {
    Write-Host "🔐 Incluyendo archivo .env en el paquete de subida..." -ForegroundColor Cyan
    Copy-Item "$proyectoDir\.env" -Destination "$publicacionDir\.env" -Force
    
    # 2.1 Forzar entorno de Producción en el archivo subido
    Write-Host "⚙️ Ajustando entorno a PRODUCCIÓN..." -ForegroundColor Cyan
    (Get-Content "$publicacionDir\.env") -replace "ASPNETCORE_ENVIRONMENT=Development", "ASPNETCORE_ENVIRONMENT=Production" | Set-Content "$publicacionDir\.env"
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

# 4.1 Crear estructura de carpetas primero
Write-Host "`n📁 Verificando/Creando estructura de carpetas en el servidor..." -ForegroundColor Yellow
$directorios = Get-ChildItem -Path $publicacionDir -Recurse | Where-Object { $_.PSIsContainer } | Sort-Object FullName

foreach ($dir in $directorios) {
    $relPath = $dir.FullName.Substring($publicacionDir.Length + 1).Replace("\", "/")
    $dirUrl = ($ftpServerBase.TrimEnd('/') + "/" + $relPath)
    
    try {
        $uri = [System.Uri]$dirUrl
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $request.GetResponse().Close()
        Write-Host "➕ Carpeta creada: $relPath" -ForegroundColor DarkGray
    } catch {
        # Ignoramos error 550 (Carpeta ya existe)
        # Write-Host "ℹ️ Carpeta ya existe: $relPath" -ForegroundColor DarkGray
    }
}

# 4.2 Subir archivos
Write-Host "`n🚚 Iniciando transferencia de archivos..." -ForegroundColor Yellow

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
        $request.UsePassive = $true
        $request.UseBinary = $true
        $request.KeepAlive = $false
        $request.Timeout = 30000 # 30 segundos timeout

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
