-- SCRIPT ADICIONAL: PROCESOS DE MEJORA (No Conformidades y Acciones)
-- Ejecute este script en su base de datos sistemacalidad_nch2728

USE `sistemacalidad_nch2728`;

-- 1. Tabla de No Conformidades
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

-- 2. Tabla de Acciones (Correctivas/Preventivas)
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
