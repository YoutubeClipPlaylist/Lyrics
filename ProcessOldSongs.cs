using NeteaseCloudMusicApi;

internal partial class Program
{
    static async Task CheckOldSongs(CloudMusicApi api)
    {
        Console.WriteLine("Start to check old songs...");

        // Download missing lyric files.
        HashSet<string> existsFiles = new DirectoryInfo("Lyrics").GetFiles()
                                                                 .Select(p => p.Name)
                                                                 .ToHashSet();
        HashSet<string> failedFiles = new();

        foreach (var lyric in Lyrics)
        {
            if (lyric.LyricId <= 0) continue;
            var filename = lyric.LyricId + ".lrc";

            if (existsFiles.Contains(filename)
                || failedFiles.Contains(filename)) continue;

            try
            {
                await DownloadLyricAndWriteFileAsync(api, lyric.LyricId);
                existsFiles.Add(filename);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to download lyric {lyric.LyricId}: {e.Message}");
                failedFiles.Add(filename);
            }
        }

        // Delete lyric files which are not in used.
        HashSet<string> usedFiles = Lyrics.Where(p => p.LyricId >= 0)
                                          .Select(p => p.LyricId + ".lrc")
                                          .Distinct()
                                          .ToHashSet();

        foreach (var file in existsFiles)
        {
            if (!usedFiles.Any(p => p == file))
            {
                File.Delete(Path.Combine("Lyrics", file));
                Console.WriteLine($"Delete {file} because it is not in used.");
            }
        }

        Console.WriteLine("Finish checking old songs.");
        Console.WriteLine($"Exist files count: {new DirectoryInfo("Lyrics").GetFiles().Length}");
        Console.WriteLine($"Failed count: {failedFiles.Count}");
    }
}
