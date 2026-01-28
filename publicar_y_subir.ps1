# SCRIPT DE PUBLICACION AUTOMATIZADA API SISTEMA CALIDAD (MODO FTP)
# Requisitos: Tener instalado el SDK de .NET 9

Write-Host "----------------------------------------------------" -ForegroundColor Cyan
Write-Host "🚀 Iniciando proceso de publicación de la API..."
Write-Host "📜 Nota: La base de datos se actualizará automáticamente al iniciar la aplicación." -ForegroundColor Cyan
Write-Host "----------------------------------------------------" -ForegroundColor Cyan

$proyectoDir = Join-Path $PSScriptRoot "SistemaCalidad.Api"
$publicacionDir = Join-Path $PSScriptRoot "publish"

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
$frontendDir = Join-Path $PSScriptRoot "frontend"
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
    
    # 2.1 Forzar entorno de Producción y Configurar BD AWS en el archivo subido
    Write-Host "⚙️ Ajustando entorno a PRODUCCIÓN con BD AWS..." -ForegroundColor Cyan
    $envContent = Get-Content "$publicacionDir\.env"
    $envContent = $envContent -replace "ASPNETCORE_ENVIRONMENT=Development", "ASPNETCORE_ENVIRONMENT=Production"
    $envContent = $envContent -replace "DB_HOST=localhost", "DB_HOST=ec2-18-222-25-254.us-east-2.compute.amazonaws.com"
    $envContent = $envContent -replace "DB_PASS=Smith.3976!", "DB_PASS=Smith3976!"
    $envContent = $envContent -replace "AI_API_URL=.*", "AI_API_URL=https://t-800.norteamericano.cl"
    $envContent | Set-Content "$publicacionDir\.env"
}

# 2.2 Copiar web.config personalizado (si existe) para debugging
if (Test-Path "$proyectoDir\web.config") {
    Write-Host "🔧 Incluyendo web.config personalizado (Logs activados)..." -ForegroundColor Cyan
    Copy-Item "$proyectoDir\web.config" -Destination "$publicacionDir\web.config" -Force
}

# 3. Pedir Contraseña
Write-Host "`n----------------------------------------------------"
Write-Host "🌐 Autenticando para: $ftpServerBase"
Write-Host "👤 Usuario: $ftpUser"
Write-Host "----------------------------------------------------"
# $ftpPass = Read-Host "Ingrese Contrasena para el usuario $ftpUser" -AsSecureString
Write-Host "🔑 Usando credenciales automáticas." -ForegroundColor DarkGray
$plainPass = "Aplicacionesichn88!"

# 3.1 Validar Credenciales antes de continuar
Write-Host "🔍 Verificando conexión FTP..." -ForegroundColor Cyan
try {
    $uri = [System.Uri]($ftpServerBase.TrimEnd('/'))
    $request = [System.Net.FtpWebRequest]::Create($uri)
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $request.Timeout = 5000 # 5 segundos para probar
    $response = $request.GetResponse()
    $response.Close()
    Write-Host "✅ Conexión exitosa, credenciales válidas." -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR FATAL DE CONEXIÓN: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Message -like "*530*") {
        Write-Host "⚠️ La contraseña es incorrecta o el usuario está bloqueado." -ForegroundColor Yellow
    }
    Write-Host "⛔ Abortando despliegue para evitar bloqueos."
    exit
}

# 4.0 Detener Aplicación (app_offline.htm)
Write-Host "`n🛑 Deteniendo aplicación en el servidor para liberar archivos..." -ForegroundColor Yellow
$offlineFile = "$publicacionDir\app_offline.htm"
Set-Content $offlineFile "<html><body><h1>Actualizando Sistema...</h1><p>Por favor espere unos momentos.</p></body></html>"

try {
    $uri = [System.Uri]($ftpServerBase.TrimEnd('/') + "/app_offline.htm")
    $request = [System.Net.FtpWebRequest]::Create($uri)
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    
    $fileBytes = [System.IO.File]::ReadAllBytes($offlineFile)
    $request.ContentLength = $fileBytes.Length
    $requestStream = $request.GetRequestStream()
    $requestStream.Write($fileBytes, 0, $fileBytes.Length)
    $requestStream.Close()
    
    Write-Host "✅ Aplicación detenida. Esperando 5 segundos..." -ForegroundColor Green
    Start-Sleep -Seconds 5
} catch {
    Write-Host "⚠️ No se pudo subir app_offline.htm (¿Quizás ya existe?): $($_.Exception.Message)" -ForegroundColor Yellow
}

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

# 4.1.1 Crear carpeta Logs explícitamente (Requerido por Serilog)
try {
    Write-Host "📂 Verificando carpeta 'Logs'..." -ForegroundColor Yellow
    $uri = [System.Uri]($ftpServerBase.TrimEnd('/') + "/Logs")
    $request = [System.Net.FtpWebRequest]::Create($uri)
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
    $request.GetResponse().Close()
    Write-Host "➕ Carpeta Logs creada." -ForegroundColor Green
} catch {
    # Ignorar si ya existe
}

# 4.2 Subir archivos
Write-Host "`n🚚 Iniciando transferencia de archivos..." -ForegroundColor Yellow

$archivos = Get-ChildItem -Path $publicacionDir -Recurse | Where-Object { ! $_.PSIsContainer }
$total = $archivos.Count
$actual = 0

foreach ($archivo in $archivos) {
    if ($archivo.Name -eq "app_offline.htm") { continue } # Ya lo subimos

    $actual++
    $nombreRelativo = $archivo.FullName.Substring($publicacionDir.Length + 1).Replace("\", "/")
    $urlDestino = ($ftpServerBase.TrimEnd('/') + "/" + $nombreRelativo)
    
    Write-Host "[$actual/$total] 📤 Enviando: $nombreRelativo ..." -ForegroundColor Gray
    
    $intentos = 0
    $subido = $false
    while (-not $subido -and $intentos -lt 3) {
        $intentos++
        try {
            $uri = [System.Uri]$urlDestino
            $request = [System.Net.FtpWebRequest]::Create($uri)
            $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)
            $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $request.UsePassive = $true
            $request.UseBinary = $true
            $request.KeepAlive = $false
            $request.Timeout = 60000 
            $request.ReadWriteTimeout = 60000

            $fileBytes = [System.IO.File]::ReadAllBytes($archivo.FullName)
            $request.ContentLength = $fileBytes.Length
            
            $requestStream = $request.GetRequestStream()
            $requestStream.Write($fileBytes, 0, $fileBytes.Length)
            $requestStream.Close()
            $requestStream.Dispose()
            
            $response = $request.GetResponse()
            $response.Close()
            $response.Dispose()
            $subido = $true
        } catch {
            if ($intentos -lt 3) {
                Write-Host "   ⚠️ Error en intento $intentos. Reintentando en 2 segundos..." -ForegroundColor Yellow
                Start-Sleep -Seconds 2
            } else {
                Write-Host "❌ Error persistente en $nombreRelativo : $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}

# 5. Reactivar Aplicación
Write-Host "`n🟢 Reactivando aplicación..." -ForegroundColor Yellow
try {
    $uri = [System.Uri]($ftpServerBase.TrimEnd('/') + "/app_offline.htm")
    $request = [System.Net.FtpWebRequest]::Create($uri)
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $plainPass)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
    $request.GetResponse().Close()
    Write-Host "✅ ¡Aplicación iniciada exitosamente!" -ForegroundColor Green
} catch {
    Write-Host "⚠️ No se pudo eliminar app_offline.htm: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n----------------------------------------------------"
Write-Host "🎉 ¡DESPLIEGUE FINALIZADO EN EL SERVIDOR!" -ForegroundColor Green
Write-Host "URL Base: $ftpServerBase"
Write-Host "----------------------------------------------------"
