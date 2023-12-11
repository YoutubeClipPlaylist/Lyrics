using Lyrics.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Lyrics.Json;

internal class JsonFileProcessor
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        TypeInfoResolver = SourceGenerationContext.Default,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true,
    };

    public static async Task<(List<ISong> Songs, List<ILyric> Lyrics)> ReadJsonFilesAsync()
    {
        try
        {
            return (await ReadPlaylistsAsync(), await ReadLyricsAsync());
        }
        catch (Exception e)
        {
            switch (e)
            {
                case JsonException:
                case NotSupportedException:
                    Console.Error.WriteLine("Failed to read the file.");
                    Environment.Exit(13);   // ERROR_INVALID_DATA
                    return default;
                default:
                    throw;
            }
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(SourceGenerationContext)} is set.")]
    private static async Task<List<ISong>> ReadPlaylistsAsync()
    {
        string[] jsoncFiles = Directory.EnumerateFiles(path: "Playlists",
                                                       searchPattern: "*list.jsonc",
                                                       enumerationOptions: new()
                                                       {
                                                           MatchCasing = MatchCasing.CaseInsensitive,
                                                           RecurseSubdirectories = true,
                                                           MaxRecursionDepth = 1,
                                                           IgnoreInaccessible = true
                                                       })
                                       .ToArray();

        List<ISong> songs = [];
        foreach (var file in jsoncFiles)
        {
            Console.WriteLine($"Reading {file}...");
            using FileStream fs = File.OpenRead(file);
            List<ISong> temp = await JsonSerializer.DeserializeAsync<List<ISong>>(fs, _jsonSerializerOptions)
                               ?? [];
            Console.WriteLine($"Loaded {temp.Count} songs.");
            songs.AddRange(temp);
        }

        Console.WriteLine($"Total: Loaded {songs.Count} songs.");
        return songs;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(SourceGenerationContext)} is set.")]
    private static async Task<List<ILyric>> ReadLyricsAsync()
    {
        string path = "Lyrics.json";
        if (!File.Exists(path))
        {
            using StreamWriter sw = File.CreateText(path);
            await sw.WriteLineAsync("[]");
            Console.WriteLine($"Create {path} because file is not exists.");
            return [];
        }

        Console.WriteLine($"Reading {path}...");

        using FileStream fs = File.OpenRead(path);
        List<ILyric> lyrics = await JsonSerializer.DeserializeAsync<List<ILyric>>(fs, _jsonSerializerOptions)
                              ?? [];
        Console.WriteLine($"Loaded {lyrics.Count} lyrics.");

        return lyrics;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(SourceGenerationContext)} is set.")]
    public static void WriteLyrics()
    {
        Console.WriteLine("Writing Lyrics.json...");
        File.WriteAllText(
            "Lyrics.json",
            JsonSerializer.Serialize(Program.Lyrics, options: _jsonSerializerOptions),
            System.Text.Encoding.UTF8);
        Console.WriteLine("Gracefully exit.");
    }
}
