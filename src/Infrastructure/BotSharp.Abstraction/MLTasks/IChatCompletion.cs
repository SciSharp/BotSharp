namespace BotSharp.Abstraction.MLTasks;

public interface IChatCompletion
{
    /// <summary>
    /// The LLM provider like Microsoft Azure, OpenAI, ClaudAI
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Set model name, one provider can consume different model or version(s)
    /// </summary>
    /// <param name="model">deployment name</param>
    void SetModelName(string model);

    Task<RoleDialogModel> GetChatCompletions(Agent agent,
        List<RoleDialogModel> conversations);

    Task<bool> GetChatCompletionsAsync(Agent agent, 
        List<RoleDialogModel> conversations, 
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting);

    Task<bool> GetChatCompletionsStreamingAsync(Agent agent, 
        List<RoleDialogModel> conversations, 
        Func<RoleDialogModel, Task> onMessageReceived);
}
