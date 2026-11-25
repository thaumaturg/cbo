using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cbo.Core.Validation;

/// <summary>
/// Reads an enum the way the API's <see cref="JsonStringEnumConverter"/> does (a name in any case, or a number), except
/// that an unknown name becomes an undefined value instead of failing as the body is read. The request validator then
/// reports it as a field error with the allowed names. Put it on the request property with <c>[JsonConverter]</c>, next
/// to a rule like <c>IsInEnum</c>. For enums backed by <see cref="int"/>.
/// </summary>
public sealed class UnknownEnumNameConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    // No enum gives a name to int.MinValue, so IsInEnum rejects it
    private static readonly TEnum s_unknown = (TEnum)Enum.ToObject(typeof(TEnum), int.MinValue);

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                string? name = reader.GetString();
                string? knownName = Enum.GetNames<TEnum>().FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                return knownName is null ? s_unknown : Enum.Parse<TEnum>(knownName);
            case JsonTokenType.Number:
                return (TEnum)Enum.ToObject(typeof(TEnum), reader.GetInt32());
            default:
                throw new JsonException($"Expected a {typeof(TEnum).Name} name or number.");
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
