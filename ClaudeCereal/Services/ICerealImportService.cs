using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface ICerealImportService
{
    Task<ImportResult> ImportAsync(Stream content, ImportFormat format, CancellationToken cancellationToken = default);
}
