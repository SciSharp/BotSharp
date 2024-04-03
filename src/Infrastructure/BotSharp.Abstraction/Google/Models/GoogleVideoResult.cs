namespace BotSharp.Abstraction.Google.Models;

public class GoogleVideoResult
{
    public string Kind { get; set; }
    public List<VideoItem> Items { get; set; } = new List<VideoItem>();
}

public class VideoItem
{
    public string Kind { get; set; }
    [JsonPropertyName("etag")]
    public string Etag { get; set; }
    public VideoItemId Id { get; set; }
    public VideoSnippet Snippet { get; set; }
}

public class VideoItemId
{
    public string Kind { get; set; }
    public string VideoId { get; set; }
}

public class VideoSnippet
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string ChannelId { get; set; }
    public string ChannelTitle { get; set; }
    public VideoThumbnails Thumbnails { get; set; }
}

public class VideoThumbnails
{
    public ThumbnailData Default { get; set; }
    public ThumbnailData Medium { get; set; }
    public ThumbnailData High { get; set; }
}

public class ThumbnailData
{
    public string Url { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
}