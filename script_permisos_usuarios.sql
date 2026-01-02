-- SCRIPT DE PERMISOS DE USUARIO PARA SISTEMA CALIDAD
-- Este script vincula los usuarios de sige_sam_v3 con los roles de Calidad

USE `sistemacalidad_nch2728`;

-- 1. Tabla de Permisos (vincula por ID de usuario externo)
CREATE TABLE IF NOT EXISTS `UsuariosPermisos` (
    `UsuarioIdExterno` INT NOT NULL,
    `Rol` VARCHAR(50) NOT NULL COMMENT 'Administrador, Escritor, Lector',
    `FechaAsignacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `Activo` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`UsuarioIdExterno`)
) ENGINE=InnoDB;

-- 2. Asignar los Administradores solicitados
-- Usamos REPLACE para evitar errores si ya existen
REPLACE INTO `UsuariosPermisos` (`UsuarioIdExterno`, `Rol`) VALUES (14399848, 'Administrador');
REPLACE INTO `UsuariosPermisos` (`UsuarioIdExterno`, `Rol`) VALUES (15668563, 'Administrador');
REPLACE INTO `UsuariosPermisos` (`UsuarioIdExterno`, `Rol`) VALUES (18172636, 'Administrador');
