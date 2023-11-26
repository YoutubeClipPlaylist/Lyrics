using Lyrics;
using Lyrics.Downloader;
using Lyrics.Models;
using Lyrics.Processor;

internal partial class Program
{
    private static List<ILyric> _lyrics = [];
    private static List<ISong> _songs = [];

    public static bool RETRY_FAILED_LYRICS { get; private set; }

    public static List<ILyric> Lyrics => _lyrics;

    public static async Task Main()
    {
        Startup.Configure(out int MAX_COUNT,
                          out bool _RETRY_FAILED_LYRICS,
                          out List<(string VideoId, int StartTime)> excludeSongs,
                          out List<string> excludeTitles,
                          out List<ILyric> lyricsFromENV);
        RETRY_FAILED_LYRICS = _RETRY_FAILED_LYRICS;

        try
        {
            (_songs, _lyrics) = await new JsonFileProcessor().ReadJsonFilesAsync();

            LyricsDownloader lyricsDownloader = new(new());

            LyricsProcessor lyricsProcessor = new(lyricsDownloader, ref _songs, ref _lyrics);
            lyricsProcessor.ProcessLyricsFromENV(lyricsFromENV);
            lyricsProcessor.RemoveExcludeSongs(excludeSongs);
            lyricsProcessor.RemoveSongsContainSpecifiedTitle(excludeTitles);
            List<ILyric> removed = [
                .. lyricsProcessor.RemoveLyricsNotContainsInSongs(),
                .. lyricsProcessor.RemoveDuplicatesLyrics()
            ];

            List<ISong> diffList = lyricsProcessor.FilterNewSongs();
            Console.WriteLine($"Get {diffList.Count} new songs.");

            if (diffList.Count > MAX_COUNT)
            {
                Console.WriteLine($"Too many songs to process. Max: {MAX_COUNT}, Actual: {diffList.Count}");
                Console.WriteLine("Please execute this program again later.");
                diffList = diffList.Take(MAX_COUNT).ToList();
            }

            SongProcessor songProcessor = new(lyricsDownloader, ref _lyrics);
            await songProcessor.ProcessNewSongs(diffList, removed);

            await lyricsProcessor.DownloadMissingLyrics();
            lyricsProcessor.RemoveLyricsNotInUsed();

            Console.WriteLine($"Exist files count: {new DirectoryInfo("Lyrics").GetFiles().Length}");
            // Lyrics.json is written when the program exits.
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Unhandled exception: " + e.Message);
            Console.Error.WriteLine(e.StackTrace);
            Environment.Exit(-1);
        }
        finally
        {
            Environment.Exit(0);
        }
    }
}
