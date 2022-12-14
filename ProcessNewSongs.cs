using Lyrics.Models;
using NeteaseCloudMusicApi;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

internal partial class Program
{
    static async Task ProcessNewSongs(CloudMusicApi api, List<ISong> diffList, List<ILyric> removed)
    {
        Random random = new();
        HashSet<int> failedIds = new();

        for (int i = 0; i < diffList.Count; i++)
        {
            ISong? song = diffList[i];
            song.Title = Regex.Replace(song.Title, @"[（「【\(\[].*[）」】\]\)]", "")
                              .Split('/')[0]
                              .Split('／')[0]
                              .Trim();
            try
            {
                // -1: Init
                // 0: Disable manually
                int songId = -1;
                string songName = string.Empty;

                // Find lyric id at local.
                ILyric? existLyric = removed.Find(p => p.Title.ToLower() == song.Title.ToLower())
                                     ?? Lyrics.Find(p => p.Title.ToLower() == song.Title.ToLower());

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

                if (failedIds.Contains(songId))
                    songId = -songId;

                if (songId > 0)
                {
                    try
                    {
                        await DownloadLyricAndWriteFileAsync(api, songId);
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

                    if (songId < -1) failedIds.Add(-songId);
                }

                Lyrics.Add(new Lyric()
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
            // 沒有時間資訊
            || !Regex.IsMatch(lyricString, @"\[\d{2}:\d{2}.\d{1,5}\]")
            // 小於6行
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

        if ((bool?)json["uncollected"] == true
            || (bool?)json["nolyric"] == true)
            return null;

        return json["lrc"]["lyric"].ToString().Trim();
    }
}
