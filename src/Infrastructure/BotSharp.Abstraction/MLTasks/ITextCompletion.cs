namespace BotSharp.Abstraction.MLTasks;

public interface ITextCompletion
{
    /// <summary>
    /// The LLM provider like Microsoft Azure, OpenAI, ClaudAI
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Set model name, one provider can consume different model or version(s)
    /// </summary>
    /// <param name="model"></param>
    void SetModelName(string model);

    Task<string> GetCompletion(string text);
}
