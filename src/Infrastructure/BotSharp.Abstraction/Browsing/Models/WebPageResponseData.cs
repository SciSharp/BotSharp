namespace BotSharp.Abstraction.Browsing.Models;

public class WebPageResponseData
{
    public string Url { get; set; } = null!;
    public string PostData { get; set; } = null!;
    public string ResponseData { get; set; } = null!;
    public bool ResponseInMemory { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{Url} {ResponseData.Length}";
    }
}
