using Lyrics.Models;
using NeteaseCloudMusicApi;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Lyrics.Downloader;

[RequiresUnreferencedCode("Newtonsoft.Json is used.")]
public partial class LyricsDownloader(CloudMusicApi cloudMusicApi)
{
    private readonly CloudMusicApi _cloudMusicApi = cloudMusicApi;

    public async Task<bool> DownloadLyricAndWriteFileAsync(long songId)
    {
        if (songId < 0)
        {
            throw new ArgumentException("SongId invalid.", nameof(songId));
        }

        // Find local .lrc file.
        if (File.Exists($"Lyrics/{songId}.lrc"))
        {
            Console.WriteLine($"Lyric file {songId}.lrc already exists.");
            return true;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(500, 1500)));

        // Download lyric by id at Netease Cloud Music.
        string? lyricString = await GetLyricAsync(songId);

        if (string.IsNullOrEmpty(lyricString))
        {
            Console.Error.WriteLine("Can't find lyric.");
            return false;
        }

        if (lyricString.Contains("纯音乐，请欣赏")
            // 沒有時間資訊
            || !TimeRegex().IsMatch(lyricString)
            // 小於6行
            || lyricString.Split('\n').Length < 6)
        {
            Console.Error.WriteLine("Found an invalid lyric.");
            return false;
        }

        await File.WriteAllTextAsync($"Lyrics/{songId}.lrc", lyricString, System.Text.Encoding.UTF8);
        Console.WriteLine($"Write new lyric file {songId}.lrc.");
        return true;
    }

    public async Task<(long songId, string songName)> GetSongIdAsync(ISong song, int offset = 0)
    {
        if (string.IsNullOrEmpty(song.Title))
        {
            throw new ArgumentException("Song Title invalid");
        }

        (bool isOk, JObject json) = await _cloudMusicApi.RequestAsync(CloudMusicApiProviders.Search,
                                                                      new Dictionary<string, object> {
                                                                         { "keywords", song.Title},
                                                                         { "type", 1 },
                                                                         { "limit", 1 },
                                                                         { "offset", offset }
                                                                      });
        if (!isOk || null == json)
        {
            Console.Error.WriteLine($"API response ${json?["code"] ?? "error"} while getting song id.");
            return (0, string.Empty);
        }

        json = (JObject)json["result"];
        return null == json
               || json["songs"] is not IEnumerable<JToken> result
                   ? (0, string.Empty)
                   : result.Select(t => ((long)t["id"], (string)t["name"]))
                           .FirstOrDefault();
    }

    public async Task<string?> GetLyricAsync(long songId)
    {
        (bool isOk, JObject json) = await _cloudMusicApi.RequestAsync(CloudMusicApiProviders.Lyric,
                                                                      new Dictionary<string, object> {
                                                                        { "id", songId }
                                                                      });
        if (!isOk || null == json)
        {
            Console.Error.WriteLine($"API response ${json?["code"] ?? "error"} while getting lyric.");
            return null;
        }

        return (bool?)json["uncollected"] != true
               && (bool?)json["nolyric"] != true
               ? json["lrc"]["lyric"].ToString().Trim()
               : null;
    }

    [GeneratedRegex(@"\[\d{2}:\d{2}.\d{1,5}\]")]
    private static partial Regex TimeRegex();
}
