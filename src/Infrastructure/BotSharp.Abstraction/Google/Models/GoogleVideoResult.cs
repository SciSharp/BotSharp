namespace BotSharp.Abstraction.Google.Models;

public class GoogleVideoResult
{
    public string Kind { get; set; }
    public IList<VideoItem> Items { get; set; } = new List<VideoItem>();
}

public class VideoItem
{
    public string Kind { get; set; }
    [JsonPropertyName("etag")]
    public string Etag { get; set; }
    public VideoItemId Id { get; set; }
}

public class VideoItemId
{
    public string Kind { get; set; }
    public string VideoId { get; set; }
}