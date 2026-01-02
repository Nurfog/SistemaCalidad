# Roadmap de Desarrollo - Sistema Calidad NCh 2728

Este documento detalla la hoja de ruta para completar la API de administración de archivos según los requisitos de la norma.

## 🟦 Fase 1: Base y Estructura (Completado ✅)
- [x] Configuración inicial del proyecto en .NET 9.
- [x] Implementación de Modelos de Datos para Documentos y Registros.
- [x] Servicio de almacenamiento de archivos local.
- [x] Controladores básicos de carga y descarga.
- [x] Versionamiento automático de documentos.

## 🟧 Fase 2: Control y Flujos de Aprobación (En Progreso 🚧)
- [ ] **Sistema de Autenticación y Roles:**
  - Implementación de JWT.
  - Roles: Admin, Encargado Calidad, Auditor, Colaborador.
- [ ] **Workflow de Aprobación:**
  - Endpoint para solicitar revisión.
  - Firma digital/electrónica simple para aprobaciones.
  - Notificaciones por correo sobre cambios de estado.
- [ ] **Validaciones de Seguridad:**
  - Control de extensiones de archivos permitidas.
  - Escaneo básico de integridad.

## 🟨 Fase 3: Procesos Específicos NCh 2728
- [ ] **Gestión de Anexos:**
  - Módulo específico para plantillas de anexos normativos.
- [ ] **Control de Documentos Externos:**
  - Registro de manuales de equipos, normativas legales vigentes, etc.
- [ ] **Auditoría de Acceso:**
  - Registro (Log) de quién consultó o descargó cada archivo.

## 🟩 Fase 4: Reportabilidad y UX
- [ ] **Tablero de Control (Dashboard):**
  - Alerta de documentos próximos a vencer o revisión anual.
  - Estadísticas de cumplimiento por área.
- [ ] **Buscador Avanzado:**
  - Filtrado por etiquetas, fechas y contenido (OCR básico opcional).
- [ ] **Exportación de Evidencia:**
  - Generación de reportes para auditorías de certificación externas.

## 🚀 Fase 5: Integración y Nube
- [ ] Soporte para Azure Blob Storage / AWS S3.
- [ ] Dockerización de la API.
- [ ] Integración con sistemas de gestión de aprendizaje (LMS) si aplica.
