namespace BotSharp.Plugin.MetaGLM.Models.RequestModels;

public class ImageRequestBase
{
    // "quality": quality,
    // "response_format": response_format,
    // "size": size,
    // "style": style,
    // "user": user,
    // public string request_id { get; private set; }
    public string model { get; private set; }
    public string prompt { get; private set; }
    // public int n { get; private set; }

    // public ImageRequestBase SetRequestId(string requestId)
    // {
    //     this.request_id = requestId;
    //     return this;
    // }

    public ImageRequestBase SetModel(string model)
    {
        this.model = model;
        return this;
    }
    public ImageRequestBase SetPrompt(string prompt)
    {
        this.prompt = prompt;
        return this;
    }
    // public ImageRequestBase SetN(int n)
    // {
    //     this.n = n;
    //     return this;
    // }
}