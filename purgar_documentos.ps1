# ============================================================
# Script de Purga Total - Sistema de Calidad
# ============================================================
# ADVERTENCIA: Este script eliminará TODOS los documentos
# de la base de datos y del bucket S3. Esta acción es IRREVERSIBLE.
# ============================================================

param(
    [string]$ApiUrl = "https://calidad.norteamericano.cl/api",
    [string]$Usuario = "14399848",
    [string]$Password = "Apocalipsis1!"
)

# Colores para output
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-Host ""
Write-ColorOutput Yellow "============================================================"
Write-ColorOutput Yellow "       PURGA TOTAL DE DOCUMENTOS - SISTEMA CALIDAD"
Write-ColorOutput Yellow "============================================================"
Write-Host ""
Write-ColorOutput Red "⚠️  ADVERTENCIA: Esta operación es IRREVERSIBLE"
Write-Host ""
Write-Host "Esta acción eliminará:"
Write-Host "  • Todos los documentos de la base de datos"
Write-Host "  • Todas las versiones históricas"
Write-Host "  • Todos los archivos físicos del bucket S3"
Write-Host "  • La base de conocimiento de IA"
Write-Host ""

# Solicitar credenciales si no se proporcionaron
if ([string]::IsNullOrEmpty($Usuario)) {
    $Usuario = Read-Host "Usuario administrador"
}

if ([string]::IsNullOrEmpty($Password)) {
    $SecurePassword = Read-Host "Contraseña" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Confirmación de seguridad
Write-Host ""
Write-ColorOutput Red "¿Estás ABSOLUTAMENTE SEGURO de que deseas continuar?"
$confirmacion1 = Read-Host "Escribe 'PURGAR' en mayúsculas para confirmar"

if ($confirmacion1 -ne "PURGAR") {
    Write-ColorOutput Yellow "`n❌ Operación cancelada por el usuario."
    exit 0
}

Write-Host ""
Write-ColorOutput Red "Última confirmación: ¿Proceder con la eliminación total?"
$confirmacion2 = Read-Host "(S/N)"

if ($confirmacion2 -ne "S" -and $confirmacion2 -ne "s") {
    Write-ColorOutput Yellow "`n❌ Operación cancelada por el usuario."
    exit 0
}

Write-Host ""
Write-ColorOutput Cyan "🔐 Autenticando..."

try {
    # 1. Obtener token de autenticación
    $loginBody = @{
        usuario = $Usuario
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$ApiUrl/Auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json" `
        -ErrorAction Stop

    $token = $loginResponse.token

    if ([string]::IsNullOrEmpty($token)) {
        throw "No se pudo obtener el token de autenticación"
    }

    Write-ColorOutput Green "✅ Autenticación exitosa"
    Write-Host ""
    Write-ColorOutput Cyan "🗑️  Iniciando proceso de purga..."
    Write-Host ""

    # 2. Ejecutar purga
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    $purgeResponse = Invoke-RestMethod -Uri "$ApiUrl/Documentos/reset-total" `
        -Method Post `
        -Headers $headers `
        -ErrorAction Stop

    # 3. Mostrar resultados
    Write-Host ""
    Write-ColorOutput Green "============================================================"
    Write-ColorOutput Green "              ✅ PURGA COMPLETADA EXITOSAMENTE"
    Write-ColorOutput Green "============================================================"
    Write-Host ""
    Write-Host "📊 Resumen de la operación:"
    Write-Host "   • Documentos eliminados: $($purgeResponse.documentosEliminados)"
    Write-Host "   • Archivos físicos eliminados: $($purgeResponse.archivosFisicosEliminados)"
    Write-Host "   • Carpetas eliminadas: $($purgeResponse.carpetasEliminadas)"
    Write-Host ""
    Write-ColorOutput Cyan "💡 El sistema está ahora limpio y listo para la carga masiva."
    Write-Host ""

} catch {
    Write-Host ""
    Write-ColorOutput Red "============================================================"
    Write-ColorOutput Red "                    ❌ ERROR EN LA PURGA"
    Write-ColorOutput Red "============================================================"
    Write-Host ""
    Write-ColorOutput Red "Detalles del error:"
    Write-Host $_.Exception.Message
    Write-Host ""
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Código HTTP: $statusCode"
        
        if ($statusCode -eq 401) {
            Write-ColorOutput Yellow "💡 Verifica que el usuario y contraseña sean correctos."
        } elseif ($statusCode -eq 403) {
            Write-ColorOutput Yellow "💡 El usuario debe tener rol de Administrador."
        }
    }
    
    Write-Host ""
    exit 1
}

Write-Host ""
Read-Host "Presiona Enter para salir"
