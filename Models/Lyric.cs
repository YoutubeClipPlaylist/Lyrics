using Lyrics.Json;
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

