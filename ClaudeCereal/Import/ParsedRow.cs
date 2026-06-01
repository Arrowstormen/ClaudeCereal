using ClaudeCereal.Models;

namespace ClaudeCereal.Import;

/// <summary>
/// Discriminated union representing one row coming out of the import parser.
/// Either the row parsed successfully (<see cref="Ok"/>) or it did not (<see cref="Err"/>).
/// The two subtypes are mutually exclusive, so callers never need to null-check both fields.
/// </summary>
public abstract record ParsedRow
{
    public sealed record Ok(CerealImportRow Row)  : ParsedRow;
    public sealed record Err(string Error)         : ParsedRow;
}
