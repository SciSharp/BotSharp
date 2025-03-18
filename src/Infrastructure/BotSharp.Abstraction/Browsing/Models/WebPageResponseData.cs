namespace BotSharp.Abstraction.Browsing.Models;

public class WebPageResponseData
{
    public string Url { get; set; } = null!;
    public string PostData { get; set; } = null!;
    public string ResponseData { get; set; } = null!;
    public bool ResponseInMemory { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Method { get; set; }
    public List<WebPageCookieData> Cookies { get; set; }
    public int ResponseCode { get; set; }

    public override string ToString()
    {
        return $"{Url} {ResponseData.Length}";
    }
}
public class WebPageCookieData
{
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string Domain { get; set; } = null!;
    public string Path { get; set; } = null!;
    public float Expires { get; set; }
}
