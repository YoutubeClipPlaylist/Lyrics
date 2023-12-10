using Lyrics.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyrics.Json;

[RequiresUnreferencedCode($"{nameof(SourceGenerationContext)} should be set for JsonOption TypeInfoResolver.")]
[RequiresDynamicCode($"{nameof(SourceGenerationContext)} should be set for JsonOption TypeInfoResolver.")]
class LyricConverter : JsonConverter<ILyric>
{
    public override Lyric Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        List<JsonElement> dto = JsonSerializer.Deserialize<List<JsonElement>>(ref reader, options) ?? [];
        Queue<JsonElement> queue = new(dto);
        Lyric lyric = new();
        try
        {
            lyric.VideoId = queue.Dequeue().GetString() ?? "";
            lyric.StartTime = queue.Dequeue().GetInt32();
            lyric.LyricId = queue.Dequeue().GetInt32();
            lyric.Title = queue.Dequeue().GetString() ?? "";
            lyric.Offset = queue.Dequeue().GetSingle();
        }
        catch (InvalidOperationException) { }

        return lyric;
    }

    public override void Write(Utf8JsonWriter writer, ILyric value, JsonSerializerOptions options)
    {
        List<JsonElement> dto = [];
        dto.Add(JsonDocument.Parse($"\"{value.VideoId}\"").RootElement);
        dto.Add(JsonDocument.Parse($"{value.StartTime}").RootElement);
        dto.Add(JsonDocument.Parse($"{value.LyricId}").RootElement);
        dto.Add(JsonDocument.Parse($"\"{value.Title.Replace("\"", "\\\"")}\"").RootElement);
        dto.Add(JsonDocument.Parse($"{value.Offset}").RootElement);

        JsonSerializer.Serialize(writer, dto, options);
    }
}

