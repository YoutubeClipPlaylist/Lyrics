namespace Lyrics.Models;

public interface IOptions
{
    public IExcludeVideo[] ExcludeVideos { get; set; }
}

public class Options : IOptions
{
    public IExcludeVideo[] ExcludeVideos { get; set; } = Array.Empty<ExcludeVideo>();
}

public interface IExcludeVideo
{
    public string VideoId { get; set; }
    public int[] StartTimes { get; set; }
}

public class ExcludeVideo : IExcludeVideo
{

    public string VideoId { get; set; } = "";
    public int[] StartTimes { get; set; } = Array.Empty<int>();
}