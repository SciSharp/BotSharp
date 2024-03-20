namespace BotSharp.Abstraction.Google.Models;

public class GoogleAddressResult
{
    public IList<GoogleAddress> Results { get; set; } = new List<GoogleAddress>();
    public string Status { get; set; }
}


public class GoogleAddress
{
    [JsonPropertyName("formatted_address")]
    public string FormatedAddress { get; set; }
}