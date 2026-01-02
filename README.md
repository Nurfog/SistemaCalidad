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

## 📂 Configuración del Proyecto

### Control de Versiones (Git)
- `.gitignore`: Excluye binarios, caches y carpetas de almacenamiento local.
- `.gitattributes`: Normalización de finales de línea.
- `Storage/.gitkeep`: Mantiene la carpeta de archivos en el repositorio.

---

## 📞 Soporte Técnico
Arquitectura diseñada para superar auditorías de certificación SENCE y casas certificadoras.
