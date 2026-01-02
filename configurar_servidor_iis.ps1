# SCRIPT DE CONFIGURACION AUTOMATICA DE IIS PARA API .NET 9
# EJECUTAR ESTO DENTRO DEL SERVIDOR WINDOWS COMO ADMINISTRADOR

# --- CONFIGURACION REQUERIDA ---
$hostHeader   = "calidad.norteamericano.cl"
$physicalPath = "C:\inetpub\wwwroot\SistemaCalidad"
$siteName     = "SistemaCalidadAPI"
$port         = 80
$appPoolName  = "SistemaCalidadPool"
# -------------------------------

Write-Host "--- Configurando Servidor para: $hostHeader ---"

# 1. Asegurar Carpeta Fisica
if (-not (Test-Path $physicalPath)) { 
    Write-Host "Creando carpeta fisica..."
    New-Item -ItemType Directory -Path $physicalPath -Force | Out-Null
}

# 2. Habilitar Caracteristicas de IIS
Write-Host "Verificando caracteristicas de IIS..."
Import-Module ServerManager
Add-WindowsFeature Web-Server, Web-Asp-Net45, Web-Mgmt-Console, Web-Http-Errors | Out-Null

# 3. Crear el Application Pool
Import-Module WebAdministration
if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    Write-Host "Creando Application Pool: $appPoolName"
    $pool = New-Item "IIS:\AppPools\$appPoolName"
    $pool.managedRuntimeVersion = "" 
    $pool | Set-Item
}

# 4. Crear el Sitio Web
if (-not (Test-Path "IIS:\Sites\$siteName")) {
    Write-Host "Creando Sitio Web: $siteName"
    New-Website -Name $siteName -Port $port -PhysicalPath $physicalPath -HostHeader $hostHeader -ApplicationPool $appPoolName | Out-Null
} else {
    Write-Host "El sitio ya existe. Actualizando..."
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $physicalPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $appPoolName
    
    Get-WebBinding -Name $siteName | Remove-WebBinding
    New-WebBinding -Name $siteName -Port $port -HostHeader $hostHeader -IPAddress "*" | Out-Null
}

# 5. Configurar Permisos
Write-Host "Configurando permisos de escritura..."
$acl = Get-Acl $physicalPath
$pName = "IIS AppPool\$appPoolName"
$pRule = New-Object System.Security.AccessControl.FileSystemAccessRule($pName, "Modify", "Allow")
$acl.SetAccessRule($pRule)
Set-Acl $physicalPath $acl

# --- MENSAJES FINALES ---
Write-Host ""
Write-Host "----------------------------------------"
Write-Host "CONFIGURACION FINALIZADA CON EXITO"
Write-Host "URL: http://$hostHeader"
Write-Host "----------------------------------------"
