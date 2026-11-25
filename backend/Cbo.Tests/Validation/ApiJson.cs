using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cbo.Tests.Validation;

/// <summary>
/// Reads a request body with the JSON settings of the API (<c>ConfigureHttpJsonOptions</c> in Cbo.Api's Program.cs).
/// </summary>
public static class ApiJson
{
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerOptions.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        RespectNullableAnnotations = true,
    };

    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, s_options)!;
}
