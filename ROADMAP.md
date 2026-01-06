# Roadmap de Desarrollo - Sistema Calidad NCh 2728

Este documento detalla la hoja de ruta para completar la API de administración de archivos según los requisitos de la norma.

## 🟦 Fase 1: Base y Estructura (Completado ✅)
- [x] Configuración inicial del proyecto en .NET 9.
- [x] Implementación de Modelos de Datos para Documentos y Registros.
- [x] Servicio de almacenamiento de archivos local.
- [x] Controladores básicos de carga y descarga.
- [x] Versionamiento automático de documentos.
- [x] Configuración de Git (`.gitignore`, `.gitattributes`).

## 🟧 Fase 2: Control y Seguridad (Completado ✅)
- [x] **Sistema de Autenticación y Roles:**
  - Implementación de JWT vinculado a `sige_sam_v3`.
  - Roles: Administrador, Escritor, Lector.
- [x] **Validación en Tiempo Real (Kill-Switch):**
  - Verificación de estado `activo` contra el sistema central en cada petición.
- [x] **Workflow de Aprobación:**
  - Endpoint para solicitar revisión (`/solicitar-revision`).
  - Endpoint de aprobación formal (`/aprobar`) exclusivo para Administradores.
  - Firma electrónica simple (registro de autoría y fecha de aprobación).
  - Filtrado de seguridad: Los lectores solo ven documentos aprobados.
  - [x] Notificaciones por correo electrónico automáticas.

## 🟨 Fase 3: Procesos Específicos NCh 2728 (En Progreso 🚧)
- [x] **Auditoría de Acceso:**
  - Registro (Log) de quién descargó cada archivo con IP y fecha (Completado ✅).
- [x] **Control de Documentos Externos:**
  - Registro de manuales de equipos y normativas externas (Completado ✅).
- [x] **Gestión de Anexos:**
  - Módulo específico para plantillas de anexos normativos (Completado ✅).

## 🟩 Fase 4: Reportabilidad y Frontend (Completado ✅)
- [x] **Tablero de Control (Dashboard):**
  - Alerta de documentos próximos a vencer o revisión anual.
  - Estadísticas de cumplimiento por área.
- [x] **Cliente Web (React):**
  - Sistema de Login vinculado a base externa.
  - Interfaz de Dashboard con gráficas funcionales.
  - Listado Maestro de Documentos con filtros.
- [x] **Status Dashboard:** Monitoreo en tiempo real de API, DB y S3 en `/status`.

## 🚀 Fase 5: Expansión de Módulos (En Progreso 🚧)
- [x] **Funcionalidad Completa:**
  - [x] Flujo de aprobación de documentos (Revisión -> Aprobación) (Completado ✅).
  - [x] Módulos de No Conformidades y Acciones de Calidad (Completado ✅).
  - [ ] Panel de Anexos y Plantillas Maestras.
  - [x] Gestión de Registros y Evidencias (Completado ✅).
- [x] **Integración Cloud:**
  - [x] Almacenamiento consolidado en Amazon S3 (Completado ✅).

## 💎 Roadmap v2: Experiencia, Seguridad e Inteligencia (Completado ✅)
- [x] **Infraestructura Robusta (Observabilidad):**
  - [x] **Logging Estructurado:** Logs detallados de errores y eventos con `Serilog` (Archivos diarios y Consola).
  - [x] **Manejo Global de Errores:** Middleware para estandarizar respuestas de error (RFC 7807) en toda la API.
- [x] **Seguridad Avanzada de Documentos:**
  - [x] **Conversión Automática:** Transformación forzada de documentos (.docx, .txt) a PDF al momento de la descarga.
  - [x] **Marcas de Agua Dinámicas:** Inserción de sello "COPIA NO CONTROLADA", Usuario, Fecha y Código en cada página del PDF descargado.
  - [x] **Políticas CORS estrictas:** Restricción de orígenes y exposición controlada de encabezados.
- [x] **Experiencia de Usuario (Premium UX):**
  - [x] **Tiempo Real:** Notificaciones instantáneas (SignalR) para solicitudes de revisión y aprobaciones.
  - [x] **Dark Mode:** Interfaz adaptable con soporte para temas Claro/Oscuro persistente.
  - [x] **Micro-interacciones:** Animaciones fluidas (Framer Motion) en transiciones y notificaciones.

## 📄 Documentación (Nueva ✅)
- [x] **Manual de Usuario:** Guía visual para el personal administrativo.
- [x] **README Técnico:** Instrucciones de despliegue y desarrollo.
