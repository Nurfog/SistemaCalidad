-- SCRIPT DE CREACION DE BASE DE DATOS NCh 2728
-- Copie este contenido directamente en MySQL Workbench

CREATE DATABASE IF NOT EXISTS `sistemacalidad_nch2728` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `sistemacalidad_nch2728`;

-- 1. Tabla de Documentos
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

-- 2. Tabla de Versiones de Documentos
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

-- 3. Tabla de Registros de Calidad
CREATE TABLE IF NOT EXISTS `RegistrosCalidad` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(255) NOT NULL,
    `Descripcion` TEXT NOT NULL,
    `Identificador` VARCHAR(100) NOT NULL,
    `RutaArchivo` VARCHAR(500) NOT NULL,
    `FechaAlmacenamiento` DATETIME NOT NULL,
    `AnosRetencion` INT NOT NULL DEFAULT 5,
    `UbicacionAlmacenamiento` VARCHAR(100) NOT NULL DEFAULT 'Digital',
    `MetodoProteccion` VARCHAR(100) NOT NULL,
    `EstaEliminado` TINYINT(1) NOT NULL DEFAULT 0,
    `FechaEliminacion` DATETIME NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB;
