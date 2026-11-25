using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cbo.Core.Validation;

namespace Cbo.Tests.Validation;

public class UnknownEnumNameConverterTests
{
    public enum Color
    {
        Red,
        Green,
    }

    public record Paint
    {
        [JsonConverter(typeof(UnknownEnumNameConverter<Color>))]
        public required Color Color { get; set; }
    }

    [Theory]
    [InlineData("\"Green\"")]
    [InlineData("\"green\"")]
    [InlineData("1")]
    public void A_name_in_any_case_or_a_number_is_read(string json)
    {
        Assert.Equal(Color.Green, ApiJson.Read<Paint>($$"""{ "color": {{json}} }""").Color);
    }

    [Theory]
    [InlineData("\"Blue\"")]
    [InlineData("\"\"")]
    [InlineData("\"1\"")]
    [InlineData("\"Red, Green\"")]
    public void An_unknown_name_becomes_an_undefined_value(string json)
    {
        Assert.False(Enum.IsDefined(ApiJson.Read<Paint>($$"""{ "color": {{json}} }""").Color));
    }

    // Left to the validator, like an unknown name
    [Fact]
    public void An_undefined_number_is_read_as_is()
    {
        Assert.Equal((Color)7, ApiJson.Read<Paint>("""{ "color": 7 }""").Color);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("1.5")]
    public void Anything_else_fails_as_the_body_is_read(string json)
    {
        Assert.ThrowsAny<JsonException>(() => ApiJson.Read<Paint>($$"""{ "color": {{json}} }"""));
    }

    [Fact]
    public void A_value_is_written_by_name()
    {
        Assert.Equal("""{"color":"Green"}""", JsonSerializer.Serialize(new Paint { Color = Color.Green }, JsonSerializerOptions.Web));
    }
}
