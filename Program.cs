using Lyrics.Models;
using NeteaseCloudMusicApi;
using Newtonsoft.Json.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

if (!int.TryParse(Environment.GetEnvironmentVariable("MAX_COUNT"), out int maxCount))
{
    maxCount = 2000;
}
if (!bool.TryParse(Environment.GetEnvironmentVariable("RETRY_FAILED_LYRICS"), out bool retryFailedLyrics))
{
    retryFailedLyrics = false;
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

    List<ISong> diffList = FilterNewSongs(songs, lyrics, retryFailedLyrics);

    if (diffList.Count > maxCount)
    {
        Console.WriteLine($"Too many songs to process. Max: {maxCount}, Actual: {diffList.Count}");
        Console.WriteLine("Please execute this program again later.");
        diffList = diffList.Take(maxCount).ToList();
    }

    CloudMusicApi api = new();
    await CheckOldSongs(api, lyrics);
    await ProcessNewSongs(api, lyrics, diffList);
}
catch (Exception e)
{
    Console.WriteLine("Unhandled exception: " + e.Message);
    Console.WriteLine(e.StackTrace);
    Environment.Exit(-1);
}
finally
{
    Environment.Exit(0);
}

void ProcessExit(object? sender, EventArgs e)
{
    Console.WriteLine("Writing Lyrics.json...");
    File.WriteAllText(
        "Lyrics.json",
        JsonSerializer.Serialize(
            lyrics.ToArray(),
            options: new()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
            }),
        System.Text.Encoding.UTF8);
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

static List<ISong> FilterNewSongs(List<ISong> songs, List<ILyric> lyrics, bool retryFailedLyrics)
{
    if (retryFailedLyrics) lyrics.RemoveAll(p => p.LyricId < 0);

    HashSet<string> lyricsHashSet = new(lyrics.Select(p => p.VideoId + p.StartTime));
    return songs.Where(p => !lyricsHashSet.Contains(p.VideoId + p.StartTime)).ToList();
}

async Task CheckOldSongs(CloudMusicApi api, List<ILyric> lyrics)
{
    Console.WriteLine("Start to check old songs...");

    // Download missing lyric files.
    HashSet<string> existsFiles = new DirectoryInfo("Lyrics").GetFiles()
                                                             .Select(p => p.Name)
                                                             .ToHashSet();
    foreach (var lyric in lyrics)
    {
        if (lyric.LyricId <= 0) continue;

        if (!existsFiles.Contains(lyric.LyricId + ".lrc"))
        {
            try
            {
                await DownloadLyricAndWriteFileAsync(api, lyric.LyricId);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to download lyric {lyric.LyricId}: {e.Message}");
                continue;
            }
        }
    }

    // Delete lyric files which are not in used.
    HashSet<string> usedFiles = lyrics.Select(p => p.LyricId + ".lrc")
                                      .Distinct()
                                      .ToHashSet();

    foreach (var file in existsFiles)
    {
        if (!usedFiles.Any(p => p == file))
        {
            File.Delete(file);
        }
    }
    Console.WriteLine("Finish checking old songs.");
}

static async Task ProcessNewSongs(CloudMusicApi api, List<ILyric> lyrics, List<ISong> diffList)
{
    Random random = new();

    for (int i = 0; i < diffList.Count; i++)
    {
        ISong? song = diffList[i];
        song.Title = Regex.Replace(song.Title, @"[「【\(\[].*[」】\]\)]", "").Trim();
        try
        {
            // -1: Init
            // 0: Disable manually
            int songId = -1;
            string songName = string.Empty;

            // Find lyric id at local.
            ILyric? existLyric = lyrics.Find(p => p.Title == song.Title);
            if (null != existLyric)
            {
                (songId, songName) = (existLyric.LyricId, existLyric.Title);
            }
            else
            // Find lyric id at Netease Cloud Music.
            {
                (songId, songName) = await GetSongIdAsync(api, song);

                // Can't find song from internet.
                if (songId == 0)
                {
                    Console.Error.WriteLine($"Can't find song. {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                    songId = -1;
                }
            }

            try
            {
                if (songId > 0) await DownloadLyricAndWriteFileAsync(api, songId);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{e.Message} {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                Console.Error.WriteLine("Try with second song match.");

                var (songId2, songName2) = await GetSongIdAsync(api, song, 1);

                // Can't find song from internet.
                if (songId2 == 0)
                {
                    Console.Error.WriteLine($"Can't find second song match. {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                    songId = -songId;
                }
                else
                {
                    try
                    {
                        await DownloadLyricAndWriteFileAsync(api, songId2);
                        (songId, songName) = (songId2, songName2);
                    }
                    catch (Exception e2)
                    {
                        Console.Error.WriteLine($"{e2.Message} {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                        Console.Error.WriteLine("Failed again with second song match.");
                        songId = -songId;
                    }
                }
            }

            lyrics.Add(new Lyric()
            {
                VideoId = song.VideoId,
                StartTime = song.StartTime,
                LyricId = songId,
                Title = Regex.Replace(songName ?? "", @"[「【\(\[].*[」】\]\)]", "").Trim(),
                Offset = 0
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
    if (songId <= 0)
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
        || lyricString.Split('\n').Length < 6)
    {
        throw new Exception("Found an invalid lyric.");
    }

    await File.WriteAllTextAsync($"Lyrics/{songId}.lrc", lyricString, System.Text.Encoding.UTF8);
    Console.WriteLine($"Write new lyric file {songId}.lrc.");
}

static async Task<(int songId, string songName)> GetSongIdAsync(CloudMusicApi api, ISong song, int offset = 0)
{
    if (string.IsNullOrEmpty(song.Title))
    {
        throw new ArgumentException("Song Title invalid");
    }

    (bool isOk, JObject json) = await api.RequestAsync(CloudMusicApiProviders.Search,
                                                       new Dictionary<string, object> {
                                                           { "keywords", song.Title},
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