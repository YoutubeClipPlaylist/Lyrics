using Lyrics.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Lyrics.Processor
{
    internal class JsonFileProcessor
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public async Task<(List<ISong> Songs, List<ILyric> Lyrics)> ReadJsonFilesAsync()
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

        async Task<List<ISong>> ReadPlaylistsAsync()
        {
            string[] jsoncFiles = Directory.GetFiles("Playlists", "*list.jsonc", SearchOption.AllDirectories);
            List<ISong> songs = new();
            foreach (var file in jsoncFiles)
            {
                Console.WriteLine($"Reading {file}...");
                using FileStream fs = File.OpenRead(file);
                List<ISong> temp = await JsonSerializer.DeserializeAsync<List<ISong>>(fs, _jsonSerializerOptions)
                                   ?? new();
                Console.WriteLine($"Loaded {temp.Count} songs.");
                songs.AddRange(temp);
            }

            Console.WriteLine($"Total: Loaded {songs.Count} songs.");
            return songs;
        }

        async Task<List<ILyric>> ReadLyricsAsync()
        {
            string path = "Lyrics.json";
            if (!File.Exists(path))
            {
                using StreamWriter sw = File.CreateText(path);
                await sw.WriteLineAsync("[]");
                Console.WriteLine($"Create {path} because file is not exists.");
                return new();
            }

            Console.WriteLine($"Reading {path}...");

            using FileStream fs = File.OpenRead(path);
            List<ILyric> lyrics = await JsonSerializer.DeserializeAsync<List<ILyric>>(fs, _jsonSerializerOptions)
                                  ?? new();
            Console.WriteLine($"Loaded {lyrics.Count} lyrics.");

            return lyrics;
        }

        public static void WriteLyrics()
        {
            Console.WriteLine("Writing Lyrics.json...");
            File.WriteAllText(
                "Lyrics.json",
                JsonSerializer.Serialize(
                    Program.Lyrics.ToArray(),
                    options: new()
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        WriteIndented = true,
                    }),
                System.Text.Encoding.UTF8);
            Console.WriteLine("Gracefully exit.");
        }
    }
}
