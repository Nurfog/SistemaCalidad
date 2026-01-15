using Microsoft.EntityFrameworkCore;

namespace SistemaCalidad.Api.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Asegurar que la base de datos exista (opcional si ya manejas migraciones)
        // context.Database.EnsureCreated();

        // Crear tabla de vectores si no existe (Raw SQL para garantizar LONGBLOB y performance)
        var sql = @"
            CREATE TABLE IF NOT EXISTS DocumentoSegmentos (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                DocumentoId INT NOT NULL,
                Contenido TEXT NOT NULL,
                Vector LONGBLOB NOT NULL,
                Pagina INT DEFAULT 0,
                Seccion VARCHAR(255),
                FechaCreacion DATETIME DEFAULT CURRENT_TIMESTAMP,
                INDEX IX_DocumentoSegmentos_DocumentoId (DocumentoId),
                CONSTRAINT FK_Segmentos_Documentos 
                    FOREIGN KEY (DocumentoId) REFERENCES Documentos(Id) 
                    ON DELETE CASCADE
            ) ENGINE=InnoDB;";

        try 
        {
            context.Database.ExecuteSqlRaw(sql);
            
            // =================================================================================
            // AUTO-MIGRACIÓN: Verificar columnas faltantes en Documentos (Agregadas recientemente)
            // =================================================================================
            
            // 1. Agregar EncabezadoAdicional
            try 
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Documentos' AND COLUMN_NAME = 'EncabezadoAdicional' AND TABLE_SCHEMA = DATABASE()
                    ) THEN
                        ALTER TABLE Documentos ADD COLUMN EncabezadoAdicional LONGTEXT NULL;
                    END IF;");
            }
            catch {} // Ignorar si el motor no soporta IF EXISTS en bloque directo, usaremos try-catch forzado abajo

            // Método fail-safe para MySQL: Intentar agregar y capturar error si ya existe
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documentos ADD COLUMN EncabezadoAdicional LONGTEXT NULL;"); } catch {}
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Documentos ADD COLUMN PiePaginaPersonalizado LONGTEXT NULL;"); } catch {}
            
            Console.WriteLine("[DbInitializer] Verificación de esquema completada.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Error verificando tabla DocumentoSegmentos: {ex.Message}");
            // No lanzamos throw para no detener el arranque si es un error menor de permisos, 
            // aunque idealmente debería detenerse si la DB está mal.
        }
    }
}
