namespace BotSharp.Abstraction.Statistics.Models;

public class BotSharpStats
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = null!;

    [JsonPropertyName("group")]
    public string Group { get; set; } = null!;

    [JsonPropertyName("data")]
    public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

    private DateTime innerRecordDate;

    [JsonPropertyName("record_date")]
    public DateTime RecordDate
    {
        get
        {
            return innerRecordDate;
        }
        set
        {
            var date = new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0);
            innerRecordDate = date;
        }
    }

    public override string ToString()
    {
        return $"{Category}-{Group}: {Data?.Count ?? 0} ({RecordDate})";
    }
}