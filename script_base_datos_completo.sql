-- SCRIPT MAESTRO DE INICIALIZACIÓN: SISTEMA DE GESTIÓN DE CALIDAD NCh 2728
-- Este script unifica todas las fases (Base, Mejora, Auditoría, Permisos y Anexos)
-- Ejecute este contenido íntegramente en su MySQL Workbench

CREATE DATABASE IF NOT EXISTS `sistemacalidad_nch2728` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `sistemacalidad_nch2728`;

-- ==========================================================
-- 1. ESTRUCTURA BASE (Documentos y Registros)
-- ==========================================================

CREATE TABLE IF NOT EXISTS `Documentos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Codigo` VARCHAR(50) NOT NULL,
    `Titulo` VARCHAR(200) NOT NULL,
    `Tipo` INT NOT NULL,
    `Area` INT NOT NULL,
    `Estado` INT NOT NULL DEFAULT 0,
    `VersionActual` INT NOT NULL DEFAULT 0,
    `FechaCreacion` DATETIME NOT NULL,
    `FechaActualizacion` DATETIME NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Documentos_Codigo` (`Codigo`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `VersionesDocumento` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DocumentoId` INT NOT NULL,
    `NumeroVersion` INT NOT NULL,
    `DescripcionCambio` TEXT NOT NULL,
    `RutaArchivo` VARCHAR(500) NOT NULL,
    `NombreArchivo` VARCHAR(255) NOT NULL,
    `TipoContenido` VARCHAR(100) NOT NULL,
    `FechaCarga` DATETIME NOT NULL,
    `CreadoPor` VARCHAR(100) NOT NULL,
    `RevisadoPor` VARCHAR(100) NULL,
    `AprobadoPor` VARCHAR(100) NULL,
    `FechaAprobacion` DATETIME NULL,
    `EsVersionActual` TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    INDEX `IX_VersionesDocumento_DocumentoId` (`DocumentoId`),
    CONSTRAINT `FK_VersionesDocumento_Documentos` 
        FOREIGN KEY (`DocumentoId`) REFERENCES `Documentos` (`Id`) 
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `RegistrosCalidad` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(255) NOT NULL,
    `Descripcion` TEXT NULL,
    `Identificador` VARCHAR(100) NOT NULL,
    `RutaArchivo` VARCHAR(500) NOT NULL,
    `FechaAlmacenamiento` DATETIME NOT NULL,
    `AnosRetencion` INT NOT NULL DEFAULT 5,
    `UbicacionAlmacenamiento` VARCHAR(100) NOT NULL DEFAULT 'Digital',
    `MetodoProteccion` VARCHAR(100) NOT NULL DEFAULT 'Acceso Restringido',
    `EstaEliminado` TINYINT(1) NOT NULL DEFAULT 0,
    `FechaEliminacion` DATETIME NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB;

-- ==========================================================
-- 2. MEJORA CONTINUA (No Conformidades y Acciones)
-- ==========================================================

CREATE TABLE IF NOT EXISTS `NoConformidades` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Folio` VARCHAR(20) NOT NULL,
    `FechaDeteccion` DATETIME NOT NULL,
    `Origen` INT NOT NULL COMMENT '0:AudInt, 1:AudExt, 2:Reclamo, 3:RevDir, 4:Incump, 5:Mejora',
    `DescripcionHallazgo` TEXT NOT NULL,
    `AnalisisCausa` TEXT NULL,
    `Estado` INT NOT NULL DEFAULT 0 COMMENT '0:Abierta, 1:Analisis, 2:Implem, 3:Verif, 4:Cerrada',
    `DetectadoPor` VARCHAR(100) NOT NULL,
    `ResponsableAnalisis` VARCHAR(100) NULL,
    `FechaCierre` DATETIME NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_NoConformidades_Folio` (`Folio`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `AccionesCalidad` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `NoConformidadId` INT NOT NULL,
    `Descripcion` TEXT NOT NULL,
    `FechaCompromiso` DATETIME NOT NULL,
    `FechaEjecucion` DATETIME NULL,
    `Responsable` VARCHAR(100) NOT NULL,
    `EsEficaz` TINYINT(1) NOT NULL DEFAULT 0,
    `ObservacionesVerificacion` TEXT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_AccionesCalidad_NoConformidadId` (`NoConformidadId`),
    CONSTRAINT `FK_Acciones_NoConformidades` 
        FOREIGN KEY (`NoConformidadId`) REFERENCES `NoConformidades` (`Id`) 
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ==========================================================
-- 3. SEGURIDAD Y AUDITORÍA
-- ==========================================================

CREATE TABLE IF NOT EXISTS `UsuariosPermisos` (
    `UsuarioIdExterno` INT NOT NULL,
    `Rol` VARCHAR(50) NOT NULL COMMENT 'Administrador, Escritor, Lector',
    `FechaAsignacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `Activo` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`UsuarioIdExterno`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `AuditoriaAccesos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Usuario` VARCHAR(100) NOT NULL,
    `Accion` VARCHAR(50) NOT NULL,
    `Entidad` VARCHAR(50) NOT NULL,
    `EntidadId` INT NOT NULL,
    `Detalle` TEXT NULL,
    `Fecha` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IpOrigen` VARCHAR(45) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Auditoria_Usuario` (`Usuario`),
    INDEX `IX_Auditoria_Fecha` (`Fecha`)
) ENGINE=InnoDB;

-- ==========================================================
-- 4. DOCUMENTACIÓN EXTERNA Y ANEXOS
-- ==========================================================

CREATE TABLE IF NOT EXISTS `DocumentosExternos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(255) NOT NULL,
    `Origen` VARCHAR(255) NOT NULL,
    `VersionExterna` VARCHAR(50) NULL,
    `FechaVigencia` DATE NULL,
    `RutaArchivo` VARCHAR(500) NOT NULL,
    `EnlaceWeb` VARCHAR(500) NULL,
    `FechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `RegistradoPor` VARCHAR(100) NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Anexos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(255) NOT NULL,
    `Codigo` VARCHAR(50) NOT NULL,
    `Descripcion` TEXT NULL,
    `RutaArchivo` VARCHAR(500) NOT NULL,
    `Formato` VARCHAR(10) NOT NULL,
    `EsObligatorio` TINYINT(1) NOT NULL DEFAULT 0,
    `FechaPublicacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UltimaActualizacion` DATETIME NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Anexos_Codigo` (`Codigo`)
) ENGINE=InnoDB;

-- ==========================================================
-- 5. DATOS INICIALES (ADMINS Y PLANTILLAS BASE)
-- ==========================================================

-- Administradores Iniciales
REPLACE INTO `UsuariosPermisos` (`UsuarioIdExterno`, `Rol`) VALUES (14399848, 'Administrador');
REPLACE INTO `UsuariosPermisos` (`UsuarioIdExterno`, `Rol`) VALUES (15668563, 'Administrador');
REPLACE INTO `UsuariosPermisos` (`UsuarioIdExterno`, `Rol`) VALUES (18172636, 'Administrador');

-- Anexos Base NCh 2728
REPLACE INTO `Anexos` (`Nombre`, `Codigo`, `Descripcion`, `Formato`, `EsObligatorio`, `RutaArchivo`) 
VALUES ('Listado Maestro de Documentos', 'ANX-SGC-001', 'Listado de todos los documentos vigentes del sistema.', 'XLSX', 1, 'Templates/listado_maestro.xlsx');

REPLACE INTO `Anexos` (`Nombre`, `Codigo`, `Descripcion`, `Formato`, `EsObligatorio`, `RutaArchivo`) 
VALUES ('Registro de Asistencia', 'ANX-SGC-002', 'Plantilla oficial para registro de firmas de alumnos.', 'DOCX', 1, 'Templates/registro_asistencia.docx');

REPLACE INTO `Anexos` (`Nombre`, `Codigo`, `Descripcion`, `Formato`, `EsObligatorio`, `RutaArchivo`) 
VALUES ('Detección de Necesidades de Capacitación', 'ANX-SGC-003', 'Formulario para levantamiento de necesidades.', 'DOCX', 0, 'Templates/deteccion_necesidades.docx');
