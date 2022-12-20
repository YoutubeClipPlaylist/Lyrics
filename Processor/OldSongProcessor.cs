using Lyrics.Downloader;
using Lyrics.Models;

namespace Lyrics.Processor;

internal class OldSongProcessor
{
    private readonly HashSet<string> _existsFiles;
    private readonly LyricsDownloader _lyricsDownloader;
    private readonly List<ILyric> _lyrics;

    public OldSongProcessor(LyricsDownloader lyricsDownloader, ref List<ILyric> lyrics)
    {
        _existsFiles = new DirectoryInfo("Lyrics").GetFiles()
                                                  .Select(p => p.Name)
                                                  .ToHashSet();
        _lyricsDownloader = lyricsDownloader;
        _lyrics = lyrics;
    }

    public async Task ProcessOldSongs()
    {
        Console.WriteLine("Start to check old songs...");

        await DownloadMissingLyrics();

        RemoveLyricsNotInUsed();

        Console.WriteLine("Finish checking old songs.");
        Console.WriteLine($"Exist files count: {new DirectoryInfo("Lyrics").GetFiles().Length}");
    }

    async Task DownloadMissingLyrics()
    {
        // Download missing lyric files.
        HashSet<string> failedFiles = new();

        foreach (var lyric in _lyrics)
        {
            if (lyric.LyricId <= 0) continue;
            var filename = lyric.LyricId + ".lrc";

            if (_existsFiles.Contains(filename)
                || failedFiles.Contains(filename)) continue;

            try
            {
                await _lyricsDownloader.DownloadLyricAndWriteFileAsync(lyric.LyricId);
                _existsFiles.Add(filename);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to download lyric {lyric.LyricId}: {e.Message}");
                failedFiles.Add(filename);
            }
        }
        Console.WriteLine($"Failed count: {failedFiles.Count}");
    }

    void RemoveLyricsNotInUsed()
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
