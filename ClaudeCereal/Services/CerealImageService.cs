namespace ClaudeCereal.Services;

public class CerealImageService : ICerealImageService
{
    private readonly Dictionary<string, string> _index;

    public CerealImageService(string imageDirectory)
    {
        _index = Directory
            .EnumerateFiles(imageDirectory)
            .ToDictionary(
                path => Slugify(Path.GetFileNameWithoutExtension(path)),
                path => path,
                StringComparer.Ordinal);
    }

    public string? GetImagePath(string cerealName) =>
        _index.TryGetValue(Slugify(cerealName), out var path) ? path : null;

    private static string Slugify(string input) =>
        new string(input.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
