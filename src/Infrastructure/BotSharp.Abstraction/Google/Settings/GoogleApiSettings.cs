namespace BotSharp.Abstraction.Google.Settings;

public class GoogleApiSettings
{
    public string ApiKey { get; set; }
    public MapSettings Map { get; set; }
    public YoutubeSettings Youtube { get; set; }
}


public class MapSettings
{
    public string Endpoint { get; set; }
    public string Components { get; set; }
}

public class YoutubeSettings
{
    public string Endpoint { get; set; }
    public string Part { get; set; }
    public string RegionCode { get; set; }
    public IList<string> Channels { get; set; }
}