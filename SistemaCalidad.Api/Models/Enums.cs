namespace SistemaCalidad.Api.Models;

public enum TipoDocumento
{
    ManualCalidad,
    Procedimiento,
    Instructivo,
    Formulario,
    DocumentoExterno,
    Otro
}

public enum EstadoDocumento
{
    Borrador,
    EnRevision,
    Aprobado,
    Obsoleto,
    Archivado
}

public enum AreaProceso
{
    Direccion,
    Comercial,
    Operacional, // Capacitación
    Apoyo,
    Administrativa
}
