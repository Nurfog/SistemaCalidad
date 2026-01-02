-- SCRIPT FASE 3: GESTIÓN DE ANEXOS Y PLANTILLAS
-- Ejecute este script en su base de datos sistemacalidad_nch2728

USE `sistemacalidad_nch2728`;

-- 1. Tabla de Anexos/Plantillas (Cláusula 4.2.3)
CREATE TABLE IF NOT EXISTS `Anexos` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(255) NOT NULL,
    `Codigo` VARCHAR(50) NOT NULL,
    `Descripcion` TEXT NULL,
    `RutaArchivo` VARCHAR(500) NOT NULL,
    `Formato` VARCHAR(10) NOT NULL COMMENT 'PDF, DOCX, XLSX',
    `EsObligatorio` TINYINT(1) NOT NULL DEFAULT 0,
    `FechaPublicacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UltimaActualizacion` DATETIME NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Anexos_Codigo` (`Codigo`)
) ENGINE=InnoDB;

-- 2. Insertar algunos anexos base de la norma NCh 2728
REPLACE INTO `Anexos` (`Nombre`, `Codigo`, `Descripcion`, `Formato`, `EsObligatorio`, `RutaArchivo`) 
VALUES ('Listado Maestro de Documentos', 'ANX-SGC-001', 'Listado de todos los documentos vigentes del sistema.', 'XLSX', 1, 'Templates/listado_maestro.xlsx');

REPLACE INTO `Anexos` (`Nombre`, `Codigo`, `Descripcion`, `Formato`, `EsObligatorio`, `RutaArchivo`) 
VALUES ('Registro de Asistencia', 'ANX-SGC-002', 'Plantilla oficial para registro de firmas de alumnos.', 'DOCX', 1, 'Templates/registro_asistencia.docx');

REPLACE INTO `Anexos` (`Nombre`, `Codigo`, `Descripcion`, `Formato`, `EsObligatorio`, `RutaArchivo`) 
VALUES ('Detección de Necesidades de Capacitación', 'ANX-SGC-003', 'Formulario para levantamiento de necesidades.', 'DOCX', 0, 'Templates/deteccion_necesidades.docx');
