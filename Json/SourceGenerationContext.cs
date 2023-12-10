using Lyrics.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

// Must read:
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0
[JsonSerializable(typeof(List<ISong>))]
[JsonSerializable(typeof(List<ILyric>))]
[JsonSerializable(typeof(List<JsonElement>))]
[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip)]
internal partial class SourceGenerationContext : JsonSerializerContext { }
