namespace ClaudeCereal.Services;

public interface ICerealImageService
{
    /// <summary>
    /// Returns the full path to the image for the given cereal name,
    /// or null if no matching image is found.
    /// </summary>
    string? GetImagePath(string cerealName);
}
