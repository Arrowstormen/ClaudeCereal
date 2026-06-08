namespace ClaudeCereal.Services;

public class CerealImageService : ICerealImageService
{
    private readonly Dictionary<string, string> _index;

    public CerealImageService(string imageDirectory)
    {
        // If the image directory is absent (e.g. first run or test environment),
        // start with an empty index rather than throwing on startup.
        _index = Directory.Exists(imageDirectory)
            ? Directory
                .EnumerateFiles(imageDirectory)
                .ToDictionary(
                    path => Slugify(Path.GetFileNameWithoutExtension(path)),
                    path => path,
                    StringComparer.Ordinal)
            : [];
    }

    public string? GetImagePath(string cerealName) =>
        _index.TryGetValue(Slugify(cerealName), out var path) ? path : null;

    private static string Slugify(string s) =>
        new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
