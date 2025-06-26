using System.Text.Json.Serialization;
using System.Text.Json;

namespace Common.Model;

public readonly record struct PlayerId
{
    public string Value { get; init; }

    public static PlayerId From(string value) => new() { Value = value };

    public override string ToString()
    {
        return Value;
    }
}

public class PlayerIdJsonConverter : JsonConverter<PlayerId>
{
    public override PlayerId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        PlayerId.From(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, PlayerId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}