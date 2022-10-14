using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyrics.Models;

[JsonConverter(typeof(LyricConverter))]
public interface ILyric
{
    int LyricId { get; set; }
    int StartTime { get; set; }
    string VideoId { get; set; }
    string Title { get; set; }
    float Offset { get; set; }
}

public class Lyric : ILyric
{
    public string VideoId { get; set; } = "";
    public int StartTime { get; set; }
    public int LyricId { get; set; }
    public string Title { get; set; } = "";
    public float Offset { get; set; }
}

class LyricConverter : JsonConverter<ILyric>
{
    public override Lyric Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        List<JsonElement> dto = JsonSerializer.Deserialize<List<JsonElement>>(ref reader, options) ?? new();
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

    public override void Write(Utf8JsonWriter writer, ILyric value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, new dynamic[]
        {
            value.VideoId,
            value.StartTime,
            value.LyricId,
            value.Title,
            value.Offset
        }, options);
}

