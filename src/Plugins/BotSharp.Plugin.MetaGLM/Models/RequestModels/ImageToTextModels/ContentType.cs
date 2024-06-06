namespace BotSharp.Plugin.MetaGLM.Models.RequestModels.ImageToTextModels;


public class ContentType
{
    public string type { get; set; }
    public string text { set; get; }
    public ImageUrlType image_url { set; get; }

    public ContentType setType(string type)
    {
        this.type = type;
        return this;
    }

    public ContentType setText(string text)
    {
        this.text = text;
        return this;
    }

    public ContentType setImageUrl(string image_url)
    {
        this.image_url = new ImageUrlType(image_url);
        return this;
    }
}