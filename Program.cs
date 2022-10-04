using Lyrics.Models;
using NeteaseCloudMusicApi;
using Newtonsoft.Json.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

if (!int.TryParse(Environment.GetEnvironmentVariable("MAXCOUNT"), out int maxCount))
{
    maxCount = 2000;
}
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
    File.WriteAllText("Lyrics.json", JsonSerializer.Serialize(
        lyrics.ToArray(),
        options: new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true,
        }));
    Console.WriteLine("Gracefully exit.");
}

static void ReadJsonFiles(List<ISong> songs, List<ILyric> lyrics)
{
    JsonSerializerOptions option = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    try
    {
        ReadPlaylists();
        ReadLyrics();
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

    void ReadPlaylists()
    {
        string[] jsoncFiles = Directory.GetFiles("Playlists", "*list.jsonc", SearchOption.AllDirectories);
        foreach (var file in jsoncFiles)
        {
            Console.WriteLine($"Reading {file}...");
            using FileStream fs = File.OpenRead(file);
            List<ISong> temp = JsonSerializer.Deserialize<List<ISong>>(fs, option) ?? new();
            Console.WriteLine($"Loaded {temp.Count} songs.");
            songs.AddRange(temp);
        }

        Console.WriteLine($"Total: Loaded {songs.Count} songs.");
    }

    void ReadLyrics()
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
        List<ILyric> temp2 = JsonSerializer.Deserialize<List<ILyric>>(fs2, option) ?? new();
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

    for (int i = 0; i < diffList.Count; i++)
    {
        ISong? song = diffList[i];
        try
        {
            int songId = 0;
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
            {
                Console.Error.WriteLine($"Can't find song. {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                continue;
            }

            try
            {
                await DownloadLyricAndWriteFileAsync(api, songId);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{e.Message} {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                Console.Error.WriteLine("Try with second song match.");

                (songId, songName) = await GetSongIdAsync(api, song, 1);
                try
                {
                    await DownloadLyricAndWriteFileAsync(api, songId);
                }
                catch (ArgumentException)
                {
                    Console.Error.WriteLine($"Can't find second song match. {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                    continue;
                }
                catch (Exception)
                {
                    Console.Error.WriteLine($"{e.Message} {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                    Console.Error.WriteLine("Failed again with second song match.");
                    continue;
                }
            }

            lyrics.Add(new Lyric()
            {
                VideoId = song.VideoId,
                StartTime = song.StartTime,
                LyricId = songId,
                Title = Regex.Unescape(songName)
            });

            Console.WriteLine($"Get lyric {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}, {songId}, {songName}");
        }
        catch (Newtonsoft.Json.JsonException e)
        {
            Console.Error.WriteLine(e);
        }
        finally
        {
            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(500, 3000)));
        }
    }
}

static async Task DownloadLyricAndWriteFileAsync(CloudMusicApi api, int songId)
{
    if (songId == 0)
    {
        throw new ArgumentException("SongId invalid.", nameof(songId));
    }

    // Find local .lrc file.
    if (File.Exists($"Lyrics/{songId}.lrc"))
    {
        Console.WriteLine($"Lyric file {songId}.lrc already exists.");
        return;
    }

    await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(500, 1500)));

    // Download lyric by id at Netease Cloud Music.
    string? lyricString = await GetLyricAsync(api, songId);

    if (string.IsNullOrEmpty(lyricString))
    {
        throw new Exception("Can't find lyric.");
    }

    if (lyricString.Contains("纯音乐，请欣赏")
        || !Regex.IsMatch(lyricString, @"\[\d{2}:\d{2}.\d{2,5}\]")
        || lyricString.Split('\n').Length < 3)
    {
        throw new Exception("Found a invalid lyric.");
    }

    await File.WriteAllTextAsync($"Lyrics/{songId}.lrc", lyricString, System.Text.Encoding.UTF8);
    Console.WriteLine($"Write new lyric file {songId}.lrc.");
}

static async Task<(int songId, string songName)> GetSongIdAsync(CloudMusicApi api, ISong song, int offset = 0)
{
    (bool isOk, JObject json) = await api.RequestAsync(CloudMusicApiProviders.Search,
                                                       new Dictionary<string, object> {
                                                           { "keywords", song.Title },
                                                           { "type", 1 },
                                                           { "limit", 1 },
                                                           { "offset", offset }
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