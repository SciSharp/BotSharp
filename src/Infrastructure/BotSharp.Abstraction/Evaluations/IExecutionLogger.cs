namespace BotSharp.Abstraction.Evaluations;

public interface IExecutionLogger
{
    void Append(string conversationId, string context);
}
