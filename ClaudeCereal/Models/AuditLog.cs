namespace ClaudeCereal.Models;

public class AuditLog
{
    public int        Id            { get; set; }
    public DateTime   Timestamp     { get; set; }
    public string     Actor         { get; set; } = null!;
    public AuditAction Action       { get; set; }
    public int        EntityId      { get; set; }
    public string     EntityName    { get; set; } = null!;

    /// <summary>
    /// Groups all entries that originated from the same HTTP request or background operation.
    /// Equals HttpContext.TraceIdentifier for web requests; a generated GUID otherwise.
    /// </summary>
    public string CorrelationId { get; set; } = null!;

    /// <summary>Field-level changes captured at the time of the operation.</summary>
    public List<AuditFieldChange> Changes { get; set; } = [];
}
