namespace ClaudeCereal.Exceptions;

/// <summary>
/// Thrown when a create operation targets a name that belongs to a soft-deleted record.
/// The caller should surface this as 409 Conflict and direct the client to an admin.
/// </summary>
public class SoftDeletedConflictException(string name)
    : InvalidOperationException($"A deleted cereal named '{name}' already exists and must be restored by an admin.");
