namespace ClaudeCereal.Models;

public class AuditFieldChange
{
    public required string  Field    { get; set; }
    public          string? OldValue { get; set; }
    public          string? NewValue { get; set; }
}
