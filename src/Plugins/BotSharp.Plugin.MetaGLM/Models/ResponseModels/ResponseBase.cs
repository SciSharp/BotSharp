namespace BotSharp.Plugin.MetaGLM.Models.ResponseModels;

public class ResponseBase
{
    public string id { get; set; }
    public string request_id { get; set; }
    public long created { get; set; }
    public string model { get; set; }
    public Dictionary<string, int> usage { get; set; }
    public ResponseChoiceItem[] choices { get; set; }
    public Dictionary<string, string> error { get; set; }

    public static ResponseBase FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ResponseBase>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}