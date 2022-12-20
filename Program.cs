using Lyrics;
using Lyrics.Downloader;
using Lyrics.Models;
using Lyrics.Processor;
using NeteaseCloudMusicApi;

internal partial class Program
{
    private static List<ILyric> _lyrics = new();
    private static List<ISong> _songs = new();

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

            LyricsProcessor lyricsProcessor = new(ref _songs, ref _lyrics);
            lyricsProcessor.ProcessLyricsFromENV(lyricsFromENV);
            lyricsProcessor.RemoveExcludeSongs(excludeSongs);
            lyricsProcessor.RemoveSongsContainSpecifiedTitle(excludeTitles);
            List<ILyric> removed = lyricsProcessor.RemoveLyricsNotContainsInSongs()
                                                  .Concat(
                                                     lyricsProcessor.RemoveDuplicatesLyrics()
                                                  ).ToList();

            List<ISong> diffList = lyricsProcessor.FilterNewSongs();
            Console.WriteLine($"Get {diffList.Count} new songs.");

            if (diffList.Count > MAX_COUNT)
            {
                Console.WriteLine($"Too many songs to process. Max: {MAX_COUNT}, Actual: {diffList.Count}");
                Console.WriteLine("Please execute this program again later.");
                diffList = diffList.Take(MAX_COUNT).ToList();
            }

            CloudMusicApi api = new();
            LyricsDownloader lyricsDownloader = new(api);

            OldSongProcessor oldSongProcessor = new(lyricsDownloader, ref _lyrics);
            await oldSongProcessor.ProcessOldSongs();

            NewSongProcessor newSongProcessor = new(lyricsDownloader, ref _lyrics);
            await newSongProcessor.ProcessNewSongs(diffList, removed);

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
