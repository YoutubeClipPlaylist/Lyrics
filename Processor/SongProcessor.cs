using Lyrics.Downloader;
using Lyrics.Models;
using System.Text.RegularExpressions;

namespace Lyrics.Processor;

internal class SongProcessor
{
    private readonly LyricsDownloader _lyricsDownloader;
    private readonly List<ILyric> _lyrics;

    public SongProcessor(LyricsDownloader lyricsDownloader, ref List<ILyric> lyrics)
    {
        _lyricsDownloader = lyricsDownloader;
        _lyrics = lyrics;
    }

    internal async Task ProcessNewSongs(List<ISong> diffList, List<ILyric> removed)
    {
        Random random = new();
        HashSet<int> failedIds = _lyrics.Where(p => p.LyricId < 0)
                                        .Select(p => p.LyricId)
                                        .ToHashSet();

        for (int i = 0; i < diffList.Count; i++)
        {
            ISong? song = diffList[i];
            song.Title = CleanTitle(song.Title);

            Console.WriteLine($"Start to get lyric {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}, {song.Title}");

            try
            {
                if (TryFindLyricAtLocal(removed, song, out int songId, out string songName))
                {
                    AddToLyrics(i, song, songId, songName);
                    continue;
                }

                Console.Error.WriteLine($"Can't find song at local.");

                (songId, songName) = await FindAndDownloadFromNeteaseCloud(song: song,
                                                                           failedIds: failedIds);

                if (songId >= 0)
                {
                    AddToLyrics(i, song, songId, songName);
                    continue;
                }

                Console.WriteLine("Try with second song match.");
                var (songId2, songName2) = await FindAndDownloadFromNeteaseCloud(song: song,
                                                                                 offset: 1,
                                                                                 failedIds: failedIds);

                // 如果是第一次第二次都是小於0，那麼用第一次的結果
                if (songId2 < 0)
                {
                    AddToLyrics(i, song, songId, songName);
                }
                else
                {
                    AddToLyrics(i, song, songId2, songName2);
                }
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

        void AddToLyrics(int i, ISong song, int songId, string songName)
        {
            _lyrics.Add(new Lyric()
            {
                VideoId = song.VideoId,
                StartTime = song.StartTime,
                LyricId = songId,
                Title = CleanTitle(songName),
                Offset = 0
            });

            Console.WriteLine($"Get lyric {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}, {songId}, {songName}");
        }
    }

    /// <summary>
    /// 由本地尋找LyricId
    /// </summary>
    /// <param name="removed"></param>
    /// <param name="song"></param>
    /// <param name="result">result</param>
    /// <returns>是否成功在本地找到</returns>
    private bool TryFindLyricAtLocal(List<ILyric> removed, ISong song, out int lyricId, out string title)
    {
        ILyric? existLyric = removed.Find(p => p.Title.ToLower() == song.Title.ToLower())
                             ?? _lyrics.Find(p => p.Title.ToLower() == song.Title.ToLower())
                             ?? null;

        (lyricId, title) = existLyric == null
                            ? (0, string.Empty)
                            : (existLyric.LyricId, existLyric.Title);

        return null != existLyric;
    }

    /// <summary>
    /// 由網易雲音樂尋找SongId
    /// </summary>
    /// <param name="song"></param>
    /// <param name="offset"></param>
    /// <returns>如果成功，songId會是正整數；如果失敗，songId會是0</returns>
    private async Task<(int songId, string songName)> FindLyricAtNeteaseCloudAsync(ISong song, int offset = 0)
    {
        // Find lyric id at Netease Cloud Music.
        (int songId, string songName) = await _lyricsDownloader.GetSongIdAsync(song, offset);

        // Can't find song from internet.
        if (songId == 0)
        {
            Console.Error.WriteLine("Cannot find song at Netease Cloud Music.");
        }
        return (songId, songName);
    }

    /// <summary>
    /// 由網易雲音樂尋找SongId，並下載歌詞
    /// </summary>
    /// <param name="song"></param>
    /// <param name="offset"></param>
    /// <returns>如果成功，songId會是正整數；如果找不到歌曲，songId會是0；如果找到歌曲但是下載失敗，songId會是負數(-songId)</returns>
    private async Task<(int songId, string songName)> FindAndDownloadFromNeteaseCloud(ISong song, int offset = 0, HashSet<int>? failedIds = null)
    {
        if (null == failedIds) failedIds = new();

        var (songId, songName) = await FindLyricAtNeteaseCloudAsync(song, offset);

        if (songId == 0 
            || failedIds.Contains(songId))
        {
            songId = -songId;
            return (songId, songName);
        }

        if (!await _lyricsDownloader.DownloadLyricAndWriteFileAsync(songId))
        {
            Console.Error.WriteLine("Cannot find lyric at Netease Cloud Music.");
            failedIds.Add(songId);
            songId = -songId;
        }
        return (songId, songName);
    }

    private static string CleanTitle(string name) => Regex.Replace(name, @"[（「【\(\[].*[）」】\]\)]", "")
                                                          .Split('/')[0]
                                                          .Split('／')[0]
                                                          .Trim();
}
