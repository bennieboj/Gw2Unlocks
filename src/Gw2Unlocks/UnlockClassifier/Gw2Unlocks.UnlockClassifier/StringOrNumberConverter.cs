using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Unlocks.UnlockClassifier;

/// <summary>
/// Reads a JSON string or number as a string. Numbers use the invariant-culture form, so an
/// old cache entry written as <c>"id": 346</c> reads as <c>"346"</c>. Writes are always strings.
/// </summary>
internal sealed class StringOrNumberConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to string."),
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
