using Lyrics.Models;
using NeteaseCloudMusicApi;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

const int maxCount = 2000;

{
    List<ISong> songs = new();
    List<ILyric> lyrics = new();

    AppDomain.CurrentDomain.ProcessExit += ProcessExit;
    Console.CancelKeyPress += ProcessExit;

#if DEBUG
    Directory.CreateDirectory("Lyrics");
    Directory.CreateDirectory("Playlists");
    File.Create("Lyrics/0.lrc").Close();
#endif

    try
    {
        ReadJsonFiles(songs, lyrics);

        List<ISong> diffList = FilterNewSongs(songs, lyrics);

        if (diffList.Count > maxCount)
        {
            Console.WriteLine($"Too many songs to process. Max: {maxCount}, Actual: {diffList.Count}");
            Console.WriteLine("Please execute this program again later.");
            diffList = diffList.Take(maxCount).ToList();
        }

        await ProcessNewSongs(lyrics, diffList);
    }
    catch (Exception e)
    {
        Console.WriteLine("Unhandled exception: " + e.Message);
        Environment.Exit(-1);
    }
    finally
    {
        Environment.Exit(0);
    }

    void ProcessExit(object? sender, EventArgs e)
    {
        Console.WriteLine("Writing Lyrics.json...");
        File.WriteAllText("Lyrics.json", JsonSerializer.Serialize(lyrics.ToArray()));
        Console.WriteLine("Gracefully exit.");
    }
}

static void ReadJsonFiles(List<ISong> songs, List<ILyric> lyrics)
{
    try
    {
        ReadPlaylists(songs);
        ReadLyrics(lyrics);
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

    static void ReadPlaylists(List<ISong> songs)
    {
        string[] jsoncFiles = Directory.GetFiles("Playlists", "*list.jsonc", SearchOption.AllDirectories);
        foreach (var file in jsoncFiles)
        {
            Console.WriteLine($"Reading {file}...");
            using FileStream fs = File.OpenRead(file);
            List<ISong> temp = JsonSerializer.Deserialize<List<ISong>>(
                fs,
                new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                }
            ) ?? new();
            Console.WriteLine($"Loaded {temp.Count} songs.");
            songs.AddRange(temp);
        }

        Console.WriteLine($"Total: Loaded {songs.Count} songs.");
    }

    static void ReadLyrics(List<ILyric> lyrics)
    {
        string path = "Lyrics.json";
        if (!File.Exists(path))
        {
            using StreamWriter fs = File.CreateText(path);
            fs.WriteLine("[]");
            Console.WriteLine($"Create {path} because file is not exists.");
            return;
        }

        Console.WriteLine($"Reading {path}...");

        using FileStream fs2 = File.OpenRead(path);
        List<ILyric> temp2 = JsonSerializer.Deserialize<List<ILyric>>(
            fs2,
            new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }
        ) ?? new();
        Console.WriteLine($"Loaded {temp2.Count} lyrics.");
        lyrics.AddRange(temp2);
    }
}

static List<ISong> FilterNewSongs(List<ISong> songs, List<ILyric> lyrics)
{
    HashSet<string> lyricsHashSet = new(lyrics.Select(p => p.VideoId + p.StartTime));
    return songs.Where(p => !lyricsHashSet.Contains(p.VideoId + p.StartTime)).ToList();
}

static async Task ProcessNewSongs(List<ILyric> lyrics, List<ISong> diffList)
{
    CloudMusicApi api = new();
    Random random = new();
    int count = 0;

    foreach (var song in diffList)
    {
        try
        {
            int songId = default;
            string songName = string.Empty;

            // Find lyric id at local.
            ILyric? existLyric = lyrics.Find(p => p.Title == song.Title);
            if (null != existLyric)
                (songId, songName) = (existLyric.LyricId, existLyric.Title);

            // Find lyric id at Netease Cloud Music.
            if (null == existLyric)
                (songId, songName) = await GetSongIdAsync(api, song);

            // Can't find lyrics from internet.
            if (default == songId)
                continue;

            // Find local .lrc file.
            if (!File.Exists($"Lyrics/{songId}.lrc"))
            {
                // Download lyric by id at Netease Cloud Music.
                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(500, 1500)));

                string? lyricString = await GetLyricAsync(api, songId);

                if (!string.IsNullOrEmpty(lyricString))
                    File.WriteAllText($"Lyrics/{songId}.lrc", lyricString, System.Text.Encoding.UTF8);
            }

            lyrics.Add(new Lyric()
            {
                VideoId = song.VideoId,
                StartTime = song.StartTime,
                LyricId = songId,
                Title = Regex.Unescape(songName)
            });

            Console.WriteLine($"Get lyric {count++}/{diffList.Count}: {song.VideoId}, {song.StartTime}, {songId}, {songName}");
        }
        catch (Newtonsoft.Json.JsonException e)
        {
            Console.Error.WriteLine(e);
        }
        await Task.Delay(TimeSpan.FromMilliseconds(random.Next(500, 3000)));
    }
}

static async Task<(int songId, string songName)> GetSongIdAsync(CloudMusicApi api, ISong song)
{
    (bool isOk, JObject json) = await api.RequestAsync(CloudMusicApiProviders.Search,
                                                       new Dictionary<string, object> {
                                                           { "keywords", song.Title },
                                                           { "type", 1 },
                                                           { "limit", 1 }
                                                       });
    if (!isOk || null == json)
    {
        Console.Error.WriteLine($"API response ${json?["code"] ?? "error"} while getting song id.");
        return default;
    }

    json = (JObject)json["result"];
    if (null == json
        || json["songs"] is not IEnumerable<JToken> result
        || !result.Any())
    {
        return default;
    }

    return result.Select(t => ((int)t["id"], (string)t["name"])).FirstOrDefault();
}

static async Task<string?> GetLyricAsync(CloudMusicApi api, int songId)
{
    (bool isOk, JObject json) = await api.RequestAsync(CloudMusicApiProviders.Lyric,
                                                       new Dictionary<string, object> {
                                                           { "id", songId }
                                                       });
    if (!isOk || null == json)
    {
        Console.Error.WriteLine($"API response ${json?["code"] ?? "error"} while getting lyric.");
        return null;
    }

    if (((bool?)json["uncollected"] == true)
        || ((bool?)json["nolyric"] == true))
        return null;

    return json["lrc"]["lyric"].ToString().Trim();
}