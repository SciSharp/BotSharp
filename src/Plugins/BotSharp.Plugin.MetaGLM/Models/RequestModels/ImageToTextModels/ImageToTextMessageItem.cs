namespace BotSharp.Plugin.MetaGLM.Models.RequestModels.ImageToTextModels;


public class ImageToTextMessageItem(string role) : MessageItem(role, null)
{
    public ContentType[] content { get; set; } = new ContentType[2];

    public ImageToTextMessageItem setText(string text)
    {
        this.content[0] = new ContentType().setType("text").setText(text);
        return this;
    }

    public ImageToTextMessageItem setImageUrl(string image_url)
    {
        this.content[1] = new ContentType().setType("Image_url").setImageUrl(image_url);
        return this;
    }
}