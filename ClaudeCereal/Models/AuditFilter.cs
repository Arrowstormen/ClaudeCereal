namespace ClaudeCereal.Models;

/// <summary>Query parameters for the GET /audit endpoint.</summary>
public record AuditFilter(
    int?         EntityId      = null,
    AuditAction? Action        = null,
    string?      Actor         = null,
    string?      CorrelationId = null,
    DateTime?    From          = null,
    DateTime?    To            = null,
    int?         Page          = null,
    int?         PageSize      = null);
