namespace Lyrics.Models;

public interface IOptions
{
    public ExcludeVideo[] ExcludeVideos { get; set; }
    public string[] ExcludeTitles { get; set; }
}

public class Options : IOptions
{
    public ExcludeVideo[] ExcludeVideos { get; set; } = [];
    public string[] ExcludeTitles { get; set; } = [];
}

public interface IExcludeVideo
{
    public string VideoId { get; set; }
    public int[] StartTimes { get; set; }
}

public class ExcludeVideo : IExcludeVideo
{

    public string VideoId { get; set; } = "";
    public int[] StartTimes { get; set; } = [];
}