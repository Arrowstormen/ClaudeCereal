namespace ClaudeCereal.Exceptions;

/// <summary>
/// Thrown when a restore operation targets a cereal that is not soft-deleted.
/// The caller should surface this as 409 Conflict.
/// </summary>
public class CerealAlreadyActiveException(int id)
    : InvalidOperationException($"Cereal {id} is already active and does not need to be restored.");
