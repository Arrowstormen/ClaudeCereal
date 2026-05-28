namespace ClaudeCereal.Models;

public record ImportResult(
    int Inserted,
    int Updated,
    IReadOnlyList<SkippedRow> Skipped
);

public record SkippedRow(int Row, string Reason);

public enum ImportFormat { Csv, Json }
