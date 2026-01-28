#!/bin/bash

# SCRIPT DE PUBLICACION AUTOMATIZADA API SISTEMA CALIDAD (MODO FTP - LINUX)
# Requisitos: .NET SDK, Node.js/npm, curl

echo "----------------------------------------------------"
echo "🚀 Iniciando proceso de publicación de la API (Linux)..."
echo "📜 Nota: La base de datos se actualizará automáticamente al iniciar la aplicación."
echo "----------------------------------------------------"

# Directorios relativos
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROYECTO_DIR="$SCRIPT_DIR/SistemaCalidad.Api"
PUBLICACION_DIR="$SCRIPT_DIR/publish"
FRONTEND_DIR="$SCRIPT_DIR/frontend"

# --- CONFIGURACION FIJA DEL SERVIDOR ---
FTP_SERVER_BASE="ftp://norteamericano.com/SistemaCalidad"
FTP_USER="desarrollo"
FTP_PASS="Aplicacionesichn88!"
# ---------------------------------------

# 1. Limpiar y Publicar
if [ -d "$PUBLICACION_DIR" ]; then
    echo "🧹 Limpiando carpeta de publicación anterior..."
    rm -rf "$PUBLICACION_DIR"
fi

echo "📦 Compilando y publicando API en modo Release..."
cd "$PROYECTO_DIR"
dotnet publish -c Release -o "$PUBLICACION_DIR" /p:PublishReadyToRun=false /p:PublishSingleFile=false

if [ $? -ne 0 ]; then
    echo "❌ Error en la compilación de la API. Abortando."
    exit 1
fi

echo "✅ Publicación local de API generada."

# 1.1 Compilar Frontend (React)
if [ -d "$FRONTEND_DIR" ]; then
    echo "⚛️ Compilando Frontend (React)..."
    cd "$FRONTEND_DIR"
    
    # Check for Linux native dependencies (Rollup/Vite)
    if [ ! -d "node_modules/@rollup/rollup-linux-x64-gnu" ]; then
        echo "⚠️ Dependencias nativas de Linux no encontradas. Ejecutando npm install..."
        npm install
    fi

    npm run build
    
    if [ $? -ne 0 ]; then
        echo "❌ Error al compilar el Frontend. Intentando limpieza profunda..."
        rm -rf node_modules package-lock.json
        npm install
        npm run build
        if [ $? -ne 0 ]; then
            echo "❌ Error persistente al compilar el Frontend. Abortando."
            exit 1
        fi
    fi
    
    # 1.2 Copiar Frontend a carpeta personalizada (frontend_app) de la publicación API
    WEB_ROOT_DIR="$PUBLICACION_DIR/frontend_app"
    mkdir -p "$WEB_ROOT_DIR"
    
    echo "📂 Copiando archivos del Frontend a $WEB_ROOT_DIR ..."
    cp -r "$FRONTEND_DIR/dist/"* "$WEB_ROOT_DIR/"
fi

# 2. Configurar .env para Producción
if [ -f "$PROYECTO_DIR/.env" ]; then
    echo "🔐 Configurando archivo .env para PRODUCCIÓN..."
    cp "$PROYECTO_DIR/.env" "$PUBLICACION_DIR/.env"
    
    # Ajustes de producción
    sed -i "s/ASPNETCORE_ENVIRONMENT=Development/ASPNETCORE_ENVIRONMENT=Production/g" "$PUBLICACION_DIR/.env"
    sed -i "s/DB_HOST=localhost/DB_HOST=ec2-18-222-25-254.us-east-2.compute.amazonaws.com/g" "$PUBLICACION_DIR/.env"
    sed -i "s/DB_PASS=Smith.3976!/DB_PASS=Smith3976!/g" "$PUBLICACION_DIR/.env"
    sed -i "s|AI_API_URL=.*|AI_API_URL=http://t-800.norteamericano.cl|g" "$PUBLICACION_DIR/.env"
fi

# Copiar web.config si existe
if [ -f "$PROYECTO_DIR/web.config" ]; then
    cp "$PROYECTO_DIR/web.config" "$PUBLICACION_DIR/web.config"
fi

# 3. Transferencia FTP via CURL
echo "🚚 Iniciando transferencia de archivos via FTP..."

# 3.1 Detener Aplicación (app_offline.htm)
echo "🛑 Deteniendo aplicación (app_offline.htm)..."
OFFLINE_FILE="$PUBLICACION_DIR/app_offline.htm"
echo "<html><body><h1>Actualizando Sistema...</h1><p>Por favor espere unos momentos.</p></body></html>" > "$OFFLINE_FILE"

curl --silent --show-error --user "$FTP_USER:$FTP_PASS" --upload-file "$OFFLINE_FILE" "$FTP_SERVER_BASE/app_offline.htm"
sleep 2

# 3.2 Subir archivos recursivamente
cd "$PUBLICACION_DIR"

# Obtener lista de archivos y subirlos (método simple para FTP)
find . -type f | while read -r file; do
    rel_path=$(echo "$file" | sed 's|^\./||')
    echo "📤 Enviando: $rel_path ..."
    # Crear carpetas remotas implícitamente si el servidor FTP lo soporta o subir directamente
    # CURL se encarga de intentar subirlo a la ruta completa
    curl --silent --show-error --user "$FTP_USER:$FTP_PASS" --ftp-create-dirs --upload-file "$file" "$FTP_SERVER_BASE/$rel_path"
done

# 3.3 Reactivar Aplicación
echo "🟢 Reactivando aplicación..."
# En IIS/FTP, es más robusto hacer CWD y luego DELE
curl --silent --show-error --user "$FTP_USER:$FTP_PASS" -Q "CWD SistemaCalidad" -Q "DELE app_offline.htm" ftp://norteamericano.com/

echo "----------------------------------------------------"
echo "🎉 ¡DESPLIEGUE FINALIZADO EN EL SERVIDOR!"
echo "----------------------------------------------------"
