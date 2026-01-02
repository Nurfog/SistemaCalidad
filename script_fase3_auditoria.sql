-- SCRIPT FASE 3: AUDITORIA Y CONTROL AVANZADO
-- Ejecute este script en su base de datos sistemacalidad_nch2728

USE `sistemacalidad_nch2728`;

-- 1. Tabla de Logs de Auditoría (Exigido por la norma para trazabilidad)
CREATE TABLE IF NOT EXISTS `AuditoriaAccesos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Usuario` VARCHAR(100) NOT NULL,
    `Accion` VARCHAR(50) NOT NULL COMMENT 'LOGIN, DESCARGA, CREACION, ELIMINACION, APROBACION',
    `Entidad` VARCHAR(50) NOT NULL COMMENT 'Documento, Registro, NoConformidad',
    `EntidadId` INT NOT NULL,
    `Detalle` TEXT NULL,
    `Fecha` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IpOrigen` VARCHAR(45) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Auditoria_Usuario` (`Usuario`),
    INDEX `IX_Auditoria_Fecha` (`Fecha`)
) ENGINE=InnoDB;

-- 2. Tabla para Documentos Externos (Cláusula 4.2.3 f)
-- Documentos que no son creados por el OTEC pero son necesarios (Normativas, Manuales de Fabricante)
CREATE TABLE IF NOT EXISTS `DocumentosExternos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(255) NOT NULL,
    `Origen` VARCHAR(255) NOT NULL COMMENT 'Ej: SENCE, Ministerio del Trabajo, Proveedor X',
    `VersionExterna` VARCHAR(50) NULL,
    `FechaVigencia` DATE NULL,
    `RutaArchivo` VARCHAR(500) NOT NULL,
    `EnlaceWeb` VARCHAR(500) NULL,
    `FechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `RegistradoPor` VARCHAR(100) NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB;
