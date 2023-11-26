using Lyrics.Downloader;
using Lyrics.Models;

namespace Lyrics.Processor;

internal class LyricsProcessor
{
    private readonly LyricsDownloader _lyricsDownloader;
    private readonly List<ISong> _songs;
    private readonly List<ILyric> _lyrics;
    private readonly HashSet<string> _existsFiles;

    public LyricsProcessor(LyricsDownloader lyricsDownloader, ref List<ISong> songs, ref List<ILyric> lyrics)
    {
        _lyricsDownloader = lyricsDownloader;
        _songs = songs;
        _lyrics = lyrics;
        _existsFiles = new DirectoryInfo("Lyrics").GetFiles()
                                                  .Select(p => p.Name)
                                                  .ToHashSet();
    }

    internal void ProcessLyricsFromENV(List<ILyric> lyricFromENV)
    {
        foreach (var item in lyricFromENV)
        {
            ILyric? match = _lyrics.Find(p => p.VideoId == item.VideoId
                                           && p.StartTime == item.StartTime);
            if (null != match)
            {
                match.Offset = item.Offset;
                //_lyrics.Insert(0, old);
            }
        }
    }

    internal void RemoveExcludeSongs(List<(string VideoId, int StartTime)> excludeSongs)
    {
        var hashSet = excludeSongs.ToHashSet();
        var count = _songs.RemoveAll(p => hashSet.Contains((p.VideoId, p.StartTime)));
        excludeSongs.Where(p => p.StartTime == -1)
                    .ToList()
                    .ForEach((excludeVideoId)
                        => count += _songs.RemoveAll(p => p.VideoId == excludeVideoId.VideoId));
        Console.WriteLine($"Exclude {count} songs from exclude list.");
    }

    internal void RemoveSongsContainSpecifiedTitle(List<string> excludeTitles)
    {
        var count = _songs.RemoveAll(p => excludeTitles.Where(p1 => p.Title.Contains(p1, StringComparison.OrdinalIgnoreCase))
                                                       .Any());
        Console.WriteLine($"Exclude {count} songs from specified title.");
    }

    internal List<ILyric> RemoveLyricsNotContainsInSongs()
    {
        var songsHashSet = _songs.Select(p => (p.VideoId, p.StartTime))
                                 .ToHashSet();

        List<ILyric> removed = [];
        for (int i = 0; i < _lyrics.Count; i++)
        {
            ILyric lyric = _lyrics[i];
            if (!songsHashSet.Contains((lyric.VideoId, lyric.StartTime)))
            {
                removed.Add(_lyrics[i]);
                _lyrics.RemoveAt(i);
                i--;
            }
        }

        Console.WriteLine($"Remove {removed.Count} lyrics because of not contains in playlists.");
        return removed;
    }

    /// <summary>
    /// Remove duplicate lyrics based on VideoId and StartTime. The first one will be used if duplicates.
    /// </summary>
    /// <returns></returns>
    internal List<ILyric> RemoveDuplicatesLyrics()
    {
        HashSet<(string, int)> set = [];
        List<ILyric> removed = [];
        for (int i = 0; i < _lyrics.Count; i++)
        {
            ILyric lyric = _lyrics[i];
            if (!set.Contains((lyric.VideoId, lyric.StartTime)))
            {
                set.Add((lyric.VideoId, lyric.StartTime));
            }
            else
            {
                removed.Add(_lyrics[i]);
                _lyrics.RemoveAt(i);
                i--;
            }
        }
        Console.WriteLine($"Remove {removed.Count} lyrics because of duplicates.");
        Console.WriteLine($"Finally get {_lyrics.Count} lyrics.");
        return removed;
    }

    /// <summary>
    /// Filter out new songs that are not included in the lyrics.
    /// </summary>
    /// <returns></returns>
    internal List<ISong> FilterNewSongs()
    {
        if (Program.RETRY_FAILED_LYRICS) _lyrics.RemoveAll(p => p.LyricId < 0);

        HashSet<string> lyricsHashSet = new(_lyrics.Select(p => p.VideoId + p.StartTime));

        return _songs.Where(p => !lyricsHashSet.Contains(p.VideoId + p.StartTime))
                     .ToList();
    }

    internal async Task DownloadMissingLyrics()
    {
        Console.WriteLine($"Start to download missing lyric files.");
        HashSet<string> failedFiles = [];

        foreach (var lyric in _lyrics)
        {
            if (lyric.LyricId <= 0) continue;
            var filename = lyric.LyricId + ".lrc";

            if (_existsFiles.Contains(filename)
                || failedFiles.Contains(filename)) continue;

            if (await _lyricsDownloader.DownloadLyricAndWriteFileAsync(lyric.LyricId))
            {
                _existsFiles.Add(filename);
            }
            else
            {
                Console.Error.WriteLine($"Failed to download lyric {lyric.LyricId}");
                failedFiles.Add(filename);
            }
        }
    }

    internal void RemoveLyricsNotInUsed()
    {
        HashSet<string> usedFiles = _lyrics.Where(p => p.LyricId >= 0)
                                           .Select(p => p.LyricId + ".lrc")
                                           .Distinct()
                                           .ToHashSet();

        foreach (var file in _existsFiles)
        {
            if (!usedFiles.Any(p => p == file))
            {
                File.Delete(Path.Combine("Lyrics", file));
                Console.WriteLine($"Delete {file} because it is not in used.");
            }
        }
    }
}
