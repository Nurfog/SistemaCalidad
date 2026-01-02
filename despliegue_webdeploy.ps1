# SCRIPT DE DESPLIEGUE VIA WEB DEPLOY (MSDEPLOY)
# Requiere que Web Deploy este instalado en el Servidor

Write-Host "----------------------------------------------------" -ForegroundColor Cyan
Write-Host "🚀 Iniciando Publicacion con Web Deploy..."
Write-Host "----------------------------------------------------" -ForegroundColor Cyan

$proyectoDir = "d:\mio\DEV\SistemaCalidad\SistemaCalidad.Api"
$sitioRemoto = "SistemaCalidadAPI"  # Nombre del sitio en el IIS

# 1. Datos del Servidor
$serverIP = Read-Host "Ingrese la IP o Host del Servidor"
$user     = Read-Host "Ingrese Usuario Administrador del Servidor"
$pass     = Read-Host "Ingrese Contrasena" -AsSecureString

# Convertir password a texto plano para el comando
$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($pass)
$plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr)

# 2. Publicar y Desplegar en un solo comando
# Este comando compila, empaqueta y sube directamente al IIS remoto
Set-Location $proyectoDir

Write-Host "`n📦 Compilando y sincronizando con el servidor..." -ForegroundColor Yellow

dotnet publish /p:PublishProfile=Default /p:Configuration=Release `
/p:PublishProtocol=MSDeploy /p:MsDeployServiceUrl="https://$($serverIP):8172/msdeploy.axd" `
/p:DeployIisAppPath="$sitioRemoto" /p:Username="$user" /p:Password="$plainPass" `
/p:AllowUntrustedCertificate=True /p:AuthType=Basic

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ ¡Sincronizacion Exitosa!" -ForegroundColor Green
    Write-Host "El sitio esta actualizado en http://calidad.norteamericano.cl"
} else {
    Write-Host "`n❌ Error en el despliegue. Verifique que Web Deploy este corriendo en el puerto 8172." -ForegroundColor Red
}
