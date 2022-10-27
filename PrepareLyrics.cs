﻿using Lyrics.Models;
using System.Text.Json;

internal partial class Program
{
    static async Task ReadJsonFilesAsync()
    {
        JsonSerializerOptions option = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        try
        {
            await ReadPlaylistsAsync();
            await ReadLyricsAsync();
        }
        catch (JsonException)
        {
            Console.WriteLine("Failed to read the file.");
            Environment.Exit(13);   // ERROR_INVALID_DATA
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("Failed to read the file.");
            Environment.Exit(13);   // ERROR_INVALID_DATA
        }

        async Task ReadPlaylistsAsync()
        {
            string[] jsoncFiles = Directory.GetFiles("Playlists", "*list.jsonc", SearchOption.AllDirectories);
            foreach (var file in jsoncFiles)
            {
                Console.WriteLine($"Reading {file}...");
                using FileStream fs = File.OpenRead(file);
                List<ISong> temp = await JsonSerializer.DeserializeAsync<List<ISong>>(fs, option) ?? new();
                Console.WriteLine($"Loaded {temp.Count} songs.");
                Songs.AddRange(temp);
            }

            Console.WriteLine($"Total: Loaded {Songs.Count} songs.");
        }

        async Task ReadLyricsAsync()
        {
            string path = "Lyrics.json";
            if (!File.Exists(path))
            {
                using StreamWriter fs = File.CreateText(path);
                await fs.WriteLineAsync("[]");
                Console.WriteLine($"Create {path} because file is not exists.");
                return;
            }

            Console.WriteLine($"Reading {path}...");

            using FileStream fs2 = File.OpenRead(path);
            List<ILyric> temp2 = await JsonSerializer.DeserializeAsync<List<ILyric>>(fs2, option) ?? new();
            Console.WriteLine($"Loaded {temp2.Count} lyrics.");
            Lyrics.AddRange(temp2);
        }
    }

    static void RemoveExcludeSongs(List<(string VideoId, int StartTime)> excludeSongs)
    {
        var hashSet = excludeSongs.ToHashSet();
        var count = Songs.RemoveAll(p => hashSet.Contains((p.VideoId, p.StartTime)));
        excludeSongs.Where(p => p.StartTime == -1)
                    .ToList()
                    .ForEach((excludeVideoId) =>
                    {
                        count += Songs.RemoveAll(p => p.VideoId == excludeVideoId.VideoId);
                    });
        Console.WriteLine($"Exclude {count} songs from exclude list.");
    }

    static void RemoveLyricsNotContainsInSongs()
    {
        var songsHashSet = Songs.Select(p => (p.VideoId, p.StartTime))
                                .ToHashSet();
        var count = Lyrics.RemoveAll(p => !songsHashSet.Contains((p.VideoId, p.StartTime)));
        Console.WriteLine($"Remove {count} lyrics because of not contains in playlists.");
    }

}
