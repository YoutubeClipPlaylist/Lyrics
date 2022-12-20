using Lyrics.Downloader;
using Lyrics.Models;
using System.Text.RegularExpressions;

namespace Lyrics.Processor;

internal class NewSongProcessor
{
    private readonly LyricsDownloader _lyricsDownloader;
    private readonly List<ILyric> _lyrics;

    public NewSongProcessor(LyricsDownloader lyricsDownloader,ref List<ILyric> lyrics)
    {
        _lyricsDownloader = lyricsDownloader;
        _lyrics = lyrics;
    }

    internal async Task ProcessNewSongs(List<ISong> diffList, List<ILyric> removed)
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
                                     ?? _lyrics.Find(p => p.Title.ToLower() == song.Title.ToLower());

                if (null != existLyric)
                {
                    (songId, songName) = (existLyric.LyricId, existLyric.Title);
                }
                else
                // Find lyric id at Netease Cloud Music.
                {
                    (songId, songName) = await _lyricsDownloader.GetSongIdAsync(song);

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
                        await _lyricsDownloader.DownloadLyricAndWriteFileAsync(songId);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"{e.Message} {i + 1}/{diffList.Count}: {song.VideoId}, {song.StartTime}");
                        Console.Error.WriteLine("Try with second song match.");

                        var (songId2, songName2) = await _lyricsDownloader.GetSongIdAsync(song, 1);

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
                                await _lyricsDownloader.DownloadLyricAndWriteFileAsync(songId2);
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

                _lyrics.Add(new Lyric()
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
}
