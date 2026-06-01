namespace ClaudeCereal.Models;

/// <summary>
/// Marker interface that opts an entity into field-level audit logging.
/// Any entity implementing this interface will have its Added and Modified
/// changes captured by <see cref="ClaudeCereal.Data.AuditInterceptor"/>.
/// </summary>
public interface IAuditable { }
