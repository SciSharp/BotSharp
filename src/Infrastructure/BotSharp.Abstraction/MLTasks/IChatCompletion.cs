namespace BotSharp.Abstraction.MLTasks;

public interface IChatCompletion
{
    /// <summary>
    /// The LLM provider like Microsoft Azure, OpenAI, ClaudAI
    /// </summary>
    string Provider { get; }

    string Model { get; }

    /// <summary>
    /// Set model name, one provider can consume different model or version(s)
    /// </summary>
    /// <param name="model">deployment name</param>
    void SetModelName(string model);

    Task<RoleDialogModel> GetChatCompletions(Agent agent,
        List<RoleDialogModel> conversations) => throw new NotImplementedException();

    Task<bool> GetChatCompletionsAsync(Agent agent, 
        List<RoleDialogModel> conversations, 
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting) => throw new NotImplementedException();

    Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, 
        List<RoleDialogModel> conversations) => throw new NotImplementedException();
}
