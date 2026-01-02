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
- [ ] **Gestión de Anexos:**
  - Módulo específico para plantillas de anexos normativos.

## 🟩 Fase 4: Reportabilidad y UX
- [ ] **Tablero de Control (Dashboard):**
  - Alerta de documentos próximos a vencer o revisión anual.
  - Estadísticas de cumplimiento por área.
- [ ] **Buscador Avanzado:**
  - Filtrado por etiquetas, fechas y contenido.
- [ ] **Exportación de Evidencia:**
  - Generación de reportes para auditorías externas.

## 🚀 Fase 5: Integración y Nube
- [ ] Soporte para Azure Blob Storage / AWS S3.
- [ ] Dockerización de la API.
- [ ] Integración con sistemas de gestión de aprendizaje (LMS).
