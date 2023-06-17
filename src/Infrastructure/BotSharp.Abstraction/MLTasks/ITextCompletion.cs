namespace BotSharp.Abstraction.MLTasks;

public interface ITextCompletion
{
    Task<string> GetCompletion(string text);
}
