using Lyrics.Json;
using Lyrics.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Lyrics;

public static class Startup
{
    static void ProcessExit(object? sender, EventArgs e)
        => JsonFileProcessor.WriteLyrics();

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(SourceGenerationContext)} is set.")]
    public static void Configure(out int MAX_COUNT,
                                 out bool RETRY_FAILED_LYRICS,
                                 out List<(string, int)> excludeSongs,
                                 out List<string> excludeTitles,
                                 out List<ILyric> lyricsFromENV)
    {
        AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        Console.CancelKeyPress += ProcessExit;

        IOptions option = ReadOptions();

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

        excludeTitles = [.. option.ExcludeTitles];

        string? lyricString = Environment.GetEnvironmentVariable("LYRICS");
        lyricsFromENV = [];
        if (!string.IsNullOrEmpty(lyricString))
        {
            try
            {
                lyricsFromENV = JsonSerializer.Deserialize<List<ILyric>>(lyricString, options: new()
                {
                    TypeInfoResolver = SourceGenerationContext.Default
                }) ?? [];
                Console.WriteLine($"Get {lyricsFromENV.Count} lyrics from ENV.");
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case JsonException:
                    case NotSupportedException:
                        Console.Error.WriteLine("Failed to parse lyric json from ENV.");
                        break;
                    default:
                        throw;
                }
            }
        }

#if DEBUG
        Directory.CreateDirectory("Lyrics");
        Directory.CreateDirectory("Playlists");
        File.Create("Lyrics/0.lrc").Close();
#endif
    }

    private static IOptions ReadOptions()
    {
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
                .AddEnvironmentVariables()
                .Build();

            IOptions? option = configuration.Get<Options>();
            if (null == option
                || null == option.ExcludeVideos)
            {
                throw new ApplicationException("Settings file is not valid.");
            }
            Console.WriteLine($"Get {option.ExcludeVideos.Length} exclude videos.");
            return option;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine("ERROR_BAD_CONFIGURATION");
            Environment.Exit(1610); // ERROR_BAD_CONFIGURATION
            return default;
        }
    }
}