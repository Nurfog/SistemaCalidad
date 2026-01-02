namespace SistemaCalidad.Api.Models;

public enum DocumentType
{
    QualityManual,
    Procedure,
    WorkInstruction,
    Form,
    ExternalDocument,
    Other
}

public enum DocumentStatus
{
    Draft,
    InReview,
    Approved,
    Obsolete,
    Archived
}

public enum ProcessArea
{
    Management,
    Commercial,
    Operational, // Training/Teaching
    Support,
    Administrative
}
