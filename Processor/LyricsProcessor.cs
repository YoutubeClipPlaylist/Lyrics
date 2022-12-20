using Lyrics.Models;

namespace Lyrics.Processor;

internal class LyricsProcessor
{
    private readonly List<ISong> _songs;
    private readonly List<ILyric> _lyrics;

    public LyricsProcessor(ref List<ISong> songs, ref List<ILyric> lyrics)
    {
        _songs = songs;
        _lyrics = lyrics;
    }

    public void ProcessLyricsFromENV(List<ILyric> lyricFromENV)
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

    public void RemoveExcludeSongs(List<(string VideoId, int StartTime)> excludeSongs)
    {
        var hashSet = excludeSongs.ToHashSet();
        var count = _songs.RemoveAll(p => hashSet.Contains((p.VideoId, p.StartTime)));
        excludeSongs.Where(p => p.StartTime == -1)
                    .ToList()
                    .ForEach((excludeVideoId)
                        => count += _songs.RemoveAll(p => p.VideoId == excludeVideoId.VideoId));
        Console.WriteLine($"Exclude {count} songs from exclude list.");
    }

    public void RemoveSongsContainSpecifiedTitle(List<string> excludeTitles)
    {
        var count = _songs.RemoveAll(p => excludeTitles.Where(p1 => p.Title.Contains(p1, StringComparison.OrdinalIgnoreCase))
                                                       .Any());
        Console.WriteLine($"Exclude {count} songs from specified title.");
    }

    public List<ILyric> RemoveLyricsNotContainsInSongs()
    {
        var songsHashSet = _songs.Select(p => (p.VideoId, p.StartTime))
                                 .ToHashSet();

        List<ILyric> removed = new();
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
    public List<ILyric> RemoveDuplicatesLyrics()
    {
        HashSet<(string, int)> set = new();
        List<ILyric> removed = new();
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
    public List<ISong> FilterNewSongs()
    {
        if (Program.RETRY_FAILED_LYRICS) _lyrics.RemoveAll(p => p.LyricId < 0);

        HashSet<string> lyricsHashSet = new(_lyrics.Select(p => p.VideoId + p.StartTime));

        return _songs.Where(p => !lyricsHashSet.Contains(p.VideoId + p.StartTime))
                     .ToList();
    }
}
