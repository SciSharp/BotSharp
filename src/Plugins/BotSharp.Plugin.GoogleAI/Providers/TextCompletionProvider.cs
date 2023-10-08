namespace BotSharp.Plugin.GoogleAI.Providers;

public class TextCompletionProvider : ITextCompletion
{
    public string Provider => "google-ai";
    private string _model;

    public Task<string> GetCompletion(string text)
    {
        throw new NotImplementedException();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
