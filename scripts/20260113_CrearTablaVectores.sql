-- Script para habilitar capacidades vectoriales locales en MySQL 8.0
-- Ejecutar este script en la base de datos 'sistemacalidad'

CREATE TABLE IF NOT EXISTS DocumentoSegmentos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    DocumentoId INT NOT NULL,
    Contenido TEXT NOT NULL,
    Vector LONGBLOB NOT NULL, -- Almacena float[] como bytes
    Pagina INT DEFAULT 0,
    Seccion VARCHAR(255),
    FechaCreacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Segmento_Documento FOREIGN KEY (DocumentoId) 
        REFERENCES Documentos(Id) ON DELETE CASCADE
);

-- Índice para búsquedas rápidas por documento
CREATE INDEX IX_DocumentoSegmentos_DocumentoId ON DocumentoSegmentos(DocumentoId);
