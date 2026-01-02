# SCRIPT DE CONFIGURACIÓN AUTOMÁTICA DE IIS PARA API .NET 9
# EJECUTAR ESTO DENTRO DEL SERVIDOR WINDOWS COMO ADMINISTRADOR

# --- CONFIGURACIÓN REQUERIDA ---
$hostHeader   = "calidad.norteamericano.cl"
$physicalPath = "C:\inetpub\wwwroot\SistemaCalidad"
$siteName     = "SistemaCalidadAPI"
$port         = 80
$appPoolName  = "SistemaCalidadPool"
# -------------------------------

Write-Host "--- Configurando Servidor para: $hostHeader ---" -ForegroundColor Cyan

# 1. Asegurar Carpeta Física
if (!(Test-Path $physicalPath)) { 
    Write-Host "Creando carpeta física..."
    New-Item -ItemType Directory -Path $physicalPath -Force
}

# 2. Habilitar Características de IIS necesarias
Write-Host "Verificando características de IIS..."
Import-Module ServerManager
Add-WindowsFeature Web-Server, Web-Asp-Net45, Web-Mgmt-Console, Web-Http-Errors

# 3. Crear el Application Pool (Modo .NET Core / No Managed Code)
Import-Module WebAdministration
if (!(Test-Path "IIS:\AppPools\$appPoolName")) {
    Write-Host "Creando Application Pool: $appPoolName"
    $pool = New-Item "IIS:\AppPools\$appPoolName"
    $pool.managedRuntimeVersion = "" # "No Managed Code" para .NET 9
    $pool | Set-Item
}

# 4. Crear el Sitio Web con Host Header (Subdominio)
if (!(Test-Path "IIS:\Sites\$siteName")) {
    Write-Host "Creando Sitio Web: $siteName en puerto $port con host $hostHeader"
    New-Website -Name $siteName -Port $port -PhysicalPath $physicalPath -HostHeader $hostHeader -ApplicationPool $appPoolName
} else {
    Write-Host "El sitio ya existe. Actualizando configuración..."
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $physicalPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $appPoolName
    
    # Actualizar Enlace (Binding)
    Get-WebBinding -Name $siteName | Remove-WebBinding
    New-WebBinding -Name $siteName -Port $port -HostHeader $hostHeader -IPAddress "*"
}

# 5. Configurar Permisos de Carpeta
Write-Host "Configurando permisos de escritura para el pool..."
$acl = Get-Acl $physicalPath
$permission = "IIS AppPool\$appPoolName","Modify","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $physicalPath $acl

Write-Host "`n--- CONFIGURACIÓN FINALIZADA CON ÉXITO ---" -ForegroundColor Green
Write-Host "El sitio debería responder en: http://$hostHeader"
Write-Host "Recuerde que el DNS del subdominio debe apuntar a la IP de este servidor."
