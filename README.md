# Sistema de Gestión de Calidad API (Norma NCh 2728:2015)

Esta API robusta ha sido desarrollada bajo el estándar **.NET 9** y está diseñada específicamente para automatizar el cumplimiento de la **Norma Chilena 2728:2015** para Organismos Técnicos de Capacitación (OTEC) en Chile. 

El sistema centraliza el control de documentos, registros de calidad y procesos de mejora continua, integrándose con sistemas de usuarios preexistentes.

---

## 🚀 Documentación Interactiva (Scalar API)

El proyecto incluye una interfaz de documentación premium basada en **Scalar**, que permite probar todos los procesos en tiempo real sin necesidad de herramientas externas.

- **URL de acceso local:** `http://localhost:5156/scalar/v1`
- **Funcionalidades en línea:**
    - Visualización de modelos de datos complejos.
    - Pruebas directas de endpoints (Try it out) con soporte para JWT.
    - Generación de código de cliente en múltiples lenguajes (JS, Python, C#, etc.).
    - Autenticación integrada para pruebas de roles.

---

## 🛠️ Procesos Normativos Implementados

### 1. Control de Documentos y Workflow (Cláusula 4.2.3)
Gestión completa del ciclo de vida documental con flujo de aprobación formal.
- **Flujo de Trabajo:**
  - `Borrador`: Estado inicial al cargar un documento (Visible solo por Escritores/Admin).
  - `En Revisión`: Solicitud formal de aprobación (`POST /solicitar-revision`).
  - `Aprobado`: Publicación oficial del documento (`POST /aprobar`). Solo accesible por Administradores.
- **Seguridad de Acceso:** Los usuarios con rol `Lector` están impedidos de ver o descargar documentos que no tengan el estado **Aprobado**.
- **Notificaciones Automáticas:** Envío de correos electrónicos a Administradores y Encargados cuando se solicita una revisión o se aprueba un documento.
- **Anexos y Plantillas:** Módulo dedicado para la descarga de formularios oficiales de la norma (Asistencia, Listados Maestros, etc.) (Cláusula 4.2.3).
- **Versionamiento:** Creación automática de nuevas versiones, manteniendo el historial completo de cambios.
- **Documentos Externos:** Módulo para el control de normativas legales, manuales de equipos o reglamentos externos (Cláusula 4.2.3 f).

### 2. Control de Registros (Cláusula 4.2.4)
Gestión de evidencias de la ejecución de procesos.
- **Retención y Disposición:** Definición de periodos de almacenamiento obligatorios.
- **Protección:** Control de acceso y respaldo de evidencia digital/física.

### 3. Mejora Continua (Cláusulas 8.3, 8.5.2, 8.5.3)
Tratamiento de No Conformidades (NC) y acciones de mejora.
- **Registro de NC:** Hallazgos con clasificación de origen (Auditoría, Reclamos, MEP, etc.).
- **Acciones Correctivas:** Planificación, ejecución y verificación de eficacia.

### 4. Auditoría de Trazabilidad (Control de Operación)
- **Logs de Acceso:** Registro inviolable de quién consultó o descargó cada documento, incluyendo IP y timestamp.
- **Historial de Operaciones:** Auditoría de inicios de sesión, cambios de estado en documentos y aprobaciones.

### 5. Buscador Avanzado (Eficiencia Operativa)
- **Filtros Multi-parámetro:** Búsqueda instantánea de documentos por código, título, área de proceso, tipo de documento o estado de aprobación.
- **Búsqueda en Registros y Anexos:** Filtrado rápido de evidencias y plantillas para soporte inmediato en auditorías.

### 6. Dashboard Normativo y Reportes (Fase 4)
- **Tablero de Control:** Visualización de estadísticas de cumplimiento: documentos por área, estado de aprobación y alertas de documentos con revisión anual vencida.
- **Status Dashboard:** Monitoreo técnico en tiempo real accesible en `/status` para verificar la conectividad de la base de datos y el almacenamiento Amazon S3.
- **Exportación de Evidencia:** Generación de reportes en formato CSV del "Listado Maestro de Documentos" y "Registro de No Conformidades", listos para ser presentados ante auditores externos de SENCE o certificadoras.

---

## 🔐 Seguridad e Integración de Usuarios

El sistema utiliza un esquema de **Seguridad Híbrida** vinculado al sistema central `sige_sam_v3`.

- **Autenticación Centralizada:** Valida identidad y contraseñas (PLANO/SHA-1) contra la tabla de usuarios central.
- **Validación de Estado "Kill-Switch":** Un Middleware verifica en **tiempo real** el estado `activo = 1` del usuario. Si es desactivado en el sistema central, pierde el acceso a la API instantáneamente.
- **Control de Acceso Basado en Roles (RBAC):**
    - `Administrador`: Control total, aprobación de documentos y gestión de auditoría.
    - `Escritor`: Carga de documentos, solicitud de revisión y gestión de No Conformidades.
    - `Lector`: Solo consulta de documentos ya aprobados y vigentes.

---

## ☁️ Almacenamiento en la Nube (Amazon S3)

El sistema soporta almacenamiento híbrido. Puede conmutar entre almacenamiento local o Amazon S3 mediante configuración:

- **Configuración en `appsettings.json`:**
  - `UseS3`: Establecer en `true` para activar AWS.
  - `BucketName`: El sistema intentará crear el bucket automáticamente si no existe.
  - `Region`, `AccessKey`, `SecretKey`: Credenciales de IAM con permisos de lectura/escritura en S3.

---

## 📂 Configuración del Proyecto

- `Storage/.gitkeep`: Mantiene la carpeta de archivos en el repositorio.

---

## ⚙️ Configuración para Desarrolladores

### Requisitos
- SDK de .NET 9.0+.
- MySQL Server 8.0+.

### Scripts de Inicialización SQL
Para dejar el sistema operativo de forma rápida, ejecute el siguiente script en su MySQL:
1. `script_base_datos_completo.sql`: Crea toda la estructura (Fase 1-4), asigna administradores y carga plantillas base.

*Nota: Los scripts individuales de cada fase permanecen en el repositorio solo como referencia histórica.*

---

## 🚀 Despliegue en Windows Server

El proyecto incluye un script de automatización para servidores Windows con **IIS**:

### Uso del Script de Publicación
1. Ejecute `publicar_y_subir.ps1` desde PowerShell.
2. El script compilará la versión final y solicitará credenciales FTP para subir los archivos.

### Requisitos en el Servidor
- **.NET 9 Hosting Bundle:** Debe estar instalado para habilitar el soporte de ASP.NET Core en IIS.
- **Configuración de IIS:** Cree un nuevo sitio web apuntando a la carpeta de destino y asegúrese de que el AppPool esté en modo **"No Managed Code"**.

---

## 📞 Soporte Técnico
Arquitectura diseñada para superar auditorías de certificación SENCE y casas certificadoras.
