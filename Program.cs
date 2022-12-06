using Lyrics;
using Lyrics.Models;
using NeteaseCloudMusicApi;

internal partial class Program
{
    public static readonly List<ILyric> Lyrics = new();
    public static readonly List<ISong> Songs = new();

    public static bool RETRY_FAILED_LYRICS { get; set; }

    private static async Task Main()
    {
        Startup.Configure(out int MAX_COUNT,
                          out bool _RETRY_FAILED_LYRICS,
                          out List<(string VideoId, int StartTime)> excludeSongs,
                          out List<string> excludeTitles,
                          out List<ILyric> lyricsFromENV);
        RETRY_FAILED_LYRICS = _RETRY_FAILED_LYRICS;

        try
        {
            await ReadJsonFilesAsync();
            ProcessLyricsFromENV(lyricsFromENV);
            RemoveExcludeSongs(excludeSongs);
            RemoveSongsContainSpecifiedTitle(excludeTitles);
            RemoveLyricsNotContainsInSongs();
            RemoveDuplicatesLyrics();

            List<ISong> diffList = FilterNewSongs();

            if (diffList.Count > MAX_COUNT)
            {
                Console.WriteLine($"Too many songs to process. Max: {MAX_COUNT}, Actual: {diffList.Count}");
                Console.WriteLine("Please execute this program again later.");
                diffList = diffList.Take(MAX_COUNT).ToList();
            }

            CloudMusicApi api = new();
            await CheckOldSongs(api);
            await ProcessNewSongs(api, diffList);
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
    }

    /// <summary>
    /// Filter out new songs that are not included in the lyrics.
    /// </summary>
    /// <returns></returns>
    static List<ISong> FilterNewSongs()
    {
        if (RETRY_FAILED_LYRICS) Lyrics.RemoveAll(p => p.LyricId < 0);

        HashSet<string> lyricsHashSet = new(Lyrics.Select(p => p.VideoId + p.StartTime));
        return Songs.Where(p => !lyricsHashSet.Contains(p.VideoId + p.StartTime)).ToList();
    }
}
