using Lyrics.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Lyrics;

public static class Startup
{
    public static void Configure(out int MAX_COUNT,
                                 out bool RETRY_FAILED_LYRICS,
                                 out List<(string, int)> excludeSongs,
                                 out List<ILyric> lyricsFromENV)
    {
        IOptions option = PrepareOptions();

        if (!int.TryParse(Environment.GetEnvironmentVariable("MAX_COUNT"), out MAX_COUNT))
        {
            MAX_COUNT = 2000;
        }
        if (!bool.TryParse(Environment.GetEnvironmentVariable("RETRY_FAILED_LYRICS"), out RETRY_FAILED_LYRICS))
        {
            RETRY_FAILED_LYRICS = false;
        }

        excludeSongs = option.ExcludeVideos.SelectMany(x => x.StartTimes.Select(y => (x.VideoId, y)))
                                           .Concat(option.ExcludeVideos.Where(p => p.StartTimes.Length == 0)
                                                                       .Select(p => (p.VideoId, -1)))
                                           .ToList();

        string? lyricString = Environment.GetEnvironmentVariable("LYRICS");
        lyricsFromENV = new();
        if (!string.IsNullOrEmpty(lyricString))
        {
            try
            {
                lyricsFromENV = JsonSerializer.Deserialize<List<ILyric>>(lyricString) ?? new();
                Console.WriteLine($"Get {lyricsFromENV.Count} lyrics from ENV.");
            }
            catch (JsonException)
            {
                Console.WriteLine("Failed to parse lyric json from ENV.");
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("Failed to parse lyric json from ENV.");
            }
        }

#if DEBUG
        Directory.CreateDirectory("Lyrics");
        Directory.CreateDirectory("Playlists");
        File.Create("Lyrics/0.lrc").Close();
#endif

        AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        Console.CancelKeyPress += ProcessExit;
    }

    static void ProcessExit(object? sender, EventArgs e)
    {
        Console.WriteLine("Writing Lyrics.json...");
        File.WriteAllText(
            "Lyrics.json",
            JsonSerializer.Serialize(
                Program.Lyrics.ToArray(),
                options: new()
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                    WriteIndented = true,
                }),
            System.Text.Encoding.UTF8);
        Console.WriteLine("Gracefully exit.");
    }

    public static IOptions PrepareOptions()
    {
        IOptions option = new Options();
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
                .AddEnvironmentVariables()
                .Build();

            option = configuration.Get<Options>();
            if (null == option
                || null == option.ExcludeVideos)
            {
                throw new ApplicationException("Settings file is not valid.");
            }
            Console.WriteLine($"Get {option.ExcludeVideos.Length} exclude videos.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("ERROR_BAD_CONFIGURATION");
            Environment.Exit(1610); // ERROR_BAD_CONFIGURATION
        }

        return option;
    }
}