# Sistema de Gestión de Calidad - API NCh 2728

Esta API está diseñada para centralizar y automatizar la administración de documentos y registros requeridos por la **Norma Chilena 2728:2015**, estándar fundamental para los Organismos Técnicos de Capacitación (OTEC) en Chile.

## 🚀 Características Principales

- **Control de Documentos (4.2.3):** Versionamiento automático, gestión de estados (Borrador, Revisión, Aprobado), y trazabilidad de cambios.
- **Control de Registros (4.2.4):** Gestión de evidencias de procesos, definición de tiempos de retención y métodos de disposición.
- **Estructura Pedagógica:** Soporte para documentos de áreas comerciales, operacionales (capacitación) y administrativas.
- **Tecnología:** Construido sobre **.NET 9** con **Entity Framework Core**.

## 🛠️ Requisitos Técnico

- .NET 9 SDK
- SQLite (incluido por defecto como base de datos local)
- Herramientas de desarrollo de C#

## 📂 Estructura del Proyecto

- `Models/`: Definiciones de Entidades y Enums según la norma.
- `Controllers/`: Endpoints de la API para Documentos y Registros.
- `Services/`: Lógica de almacenamiento y manejo de archivos.
- `Data/`: Contexto de base de datos y migraciones.

## ⚙️ Configuración y Ejecución

1. **Restaurar dependencias:**
   ```bash
   dotnet restore
   ```

2. **Ejecutar la API:**
   ```bash
   dotnet run --project SistemaCalidad.Api
   ```

3. **Acceder a la documentación (Swagger/OpenAPI):**
   La API incluye soporte nativo para OpenAPI. Al ejecutar en modo desarrollo, puedes consultar la documentación técnica en los endpoints configurados.

## 🔐 Cumplimiento Normativo (NCh 2728)

- **4.2.3.a:** Revisión y aprobación de documentos.
- **4.2.3.b:** Revisión, actualización y reaprobación.
- **4.2.3.c:** Versiones vigentes disponibles en puntos de uso.
- **4.2.4:** Legibilidad, identificación y recuperación de registros.

---
Desarrollado para el cumplimiento de estándares de calidad en capacitación.
