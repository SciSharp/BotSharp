namespace BotSharp.Plugin.MetaGLM.Models.ResponseModels.ImageGenerationModels;

public class ImageResponseBase
{
    public long created { get; set; }
    public List<ImageResponseDataItem> data { get; set; }
    public Dictionary<string, string> error { get; set; }

    public static ImageResponseBase FromJson(string json)
    {
        return JsonSerializer.Deserialize<ImageResponseBase>(json);
    }
}