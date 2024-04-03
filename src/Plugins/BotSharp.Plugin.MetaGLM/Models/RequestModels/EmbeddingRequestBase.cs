namespace BotSharp.Plugin.MetaGLM.Models.RequestModels;

public class EmbeddingRequestBase
{
    // "input": input,
    // "model": model,
    // "encoding_format": encoding_format,
    // "user": user,
    public string model { get; private set; }
    public string input { get; private set; }

    public EmbeddingRequestBase SetModel(string model)
    {
        this.model = model;
        return this;
    }
    public EmbeddingRequestBase SetInput(string input)
    {
        this.input = input;
        return this;
    }
}