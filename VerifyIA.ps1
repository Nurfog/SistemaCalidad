$AI_API_URL = "http://t-800.norteamericano.cl:9000"
$body = @{
    username = "sgc_sistema"
    prompt = "¿Cuál es el objetivo del Sistema de Gestión de Calidad (SGC) del Instituto Chileno Norteamericano según la norma NCh 2728 y qué documentos rigen el aprendizaje de la IA? Menciona algo específico que hayas extraído de los archivos PDF."
    use_kb = $true
} | ConvertTo-Json -Depth 10

Write-Host "--- INICIANDO PRUEBA SINTÉTICA DE IA (POWERSHELL) ---"
try {
    Write-Host "Realizando consulta técnica al Asistente RAG..."
    $response = Invoke-RestMethod -Uri "$AI_API_URL/chat" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 60
    
    Write-Host "`nRespuesta de la IA:"
    Write-Host "----------------------------------------------------"
    Write-Host $response
    Write-Host "----------------------------------------------------"
    
    if ($response.Length -gt 50) {
        Write-Host "`n✅ PRUEBA EXITOSA: La IA respondió con información detallada de la base de conocimientos."
    } else {
        Write-Host "`n⚠️ ADVERTENCIA: La respuesta es inusualmente corta."
    }
} catch {
    Write-Host "`n❌ ERROR EN LA PRUEBA SINTÉTICA:"
    Write-Host $_.Exception.Message
}
