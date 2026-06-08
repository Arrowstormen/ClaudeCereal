using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCereal.Tests.Helpers;

/// <summary>Shared JSON options that match the app's global serializer settings.</summary>
internal static class TestJsonOptions
{
    internal static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
