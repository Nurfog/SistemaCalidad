# Sistema de Gestión de Calidad API (Norma NCh 2728:2015)

Esta API robusta ha sido desarrollada bajo el estándar **.NET 9** y está diseñada específicamente para automatizar el cumplimiento de la **Norma Chilena 2728:2015** para Organismos Técnicos de Capacitación (OTEC) en Chile. 

El sistema centraliza el control de documentos, registros de calidad y procesos de mejora continua, integrándose con sistemas de usuarios preexistentes.

---

## 🚀 Documentación Interactiva (Scalar API)

El proyecto incluye una interfaz de documentación premium basada en **Scalar**, que permite probar todos los procesos en tiempo real sin necesidad de herramientas externas.

- **URL de acceso local:** `http://localhost:5156/scalar/v1`
- **Funcionalidades en línea:**
    - Visualización de modelos de datos.
    - Pruebas directas de endpoints (Try it out).
    - Generación de código de cliente en múltiples lenguajes (JS, Python, C#, etc.).
    - Autenticación integrada.

---

## 🛠️ Procesos Normativos Implementados

### 1. Control de Documentos (Cláusula 4.2.3)
Permite la gestión del ciclo de vida de la documentación del SGC (Manuales, Procedimientos, Instructivos).
- **Versionamiento:** Creación automática de nuevas versiones, manteniendo el historial completo.
- **Estados:** Manejo de estados: *Borrador, En Revisión, Aprobado y Obsoleto*.
- **Trazabilidad:** Registro de quién creó, revisó y aprobó cada documento.

### 2. Control de Registros (Cláusula 4.2.4)
Gestión de evidencias de la ejecución de procesos.
- **Retención:** Configuración de años de retención obligatorios.
- **Protección:** Registro de métodos de protección y respaldo de la información.

### 3. Mejora Continua (Cláusulas 8.3, 8.5.2, 8.5.3)
Módulo para el tratamiento de fallas y oportunidades de mejora.
- **No Conformidades:** Registro detallado de hallazgos con análisis de causa raíz.
- **Acciones Correctivas:** Planificación de acciones con responsables y validación de eficacia.

---

## 🔐 Seguridad e Integración de Usuarios

El sistema utiliza un esquema de **Seguridad Híbrida** vinculado al sistema central `sige_sam_v3`.

- **Autenticación Centralizada:** Valida identidad y contraseñas (PLANO/SHA-1) contra la base de datos central.
- **Validación de Estado Automática:** Si un usuario es marcado como `activo = 0` en el sistema central, pierde el acceso a la API de calidad de forma **instantánea** (inclusive en sesiones activas).
- **Roles y Permisos:**
    - `Administrador`: Acceso total y gestión de permisos.
    - `Escritor`: Permiso para crear y modificar documentos y acciones.
    - `Lector`: Acceso de solo consulta a la documentación vigente.

---

## 📂 Estructura del Código

- **`Controllers/`**: Endpoints RESTful organizados por dominio (Documentos, Registros, NoConformidades).
- **`Models/`**: Entidades y Enums que reflejan la terminología de la norma NCh 2728.
- **`Services/`**: Lógica de negocio, almacenamiento de archivos y autenticación.
- **`Data/`**: Contexto de base de datos multi-schema (MySQL).

---

## ⚙️ Configuración para Desarrolladores

### Requisitos
- SDK de .NET 9.0 o superior.
- MySQL Server 8.0+.

### Conexión a Base de Datos
El sistema utiliza archivos de configuración según el entorno:
1. `appsettings.Development.json`: Configurado para conectar a la base de datos de Desarrollo (**AWS EC2**).
2. `appsettings.json`: Configurado para el entorno de Producción (**Localhost**).

### Scripts de Inicialización
En la raíz del proyecto se encuentran los scripts SQL necesarios para preparar la base de datos:
- `script_creacion_bd.sql`: Estructura principal.
- `script_fase2_mejora.sql`: Tablas de No Conformidades y Acciones.
- `script_permisos_usuarios.sql`: Vinculación de usuarios y roles iniciales.

---

## 📞 Soporte Técnico
Desarrollado para el cumplimiento normativo riguroso y la eficiencia operativa.
