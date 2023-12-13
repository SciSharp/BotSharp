namespace BotSharp.Abstraction.MLTasks.Settings;

public class LlmModelSetting
{
    public string Name { get; set; }
    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
    public LlmModelType Type { get; set; } = LlmModelType.Chat;

    /// <summary>
    /// Prompt cost per 1K token
    /// </summary>
    public float PromptCost { get; set; }

    /// <summary>
    /// Completion cost per 1K token
    /// </summary>
    public float CompletionCost { get; set; }

    public override string ToString()
    {
        return $"[{Type}] {Name} {Endpoint}";
    }
}

public enum LlmModelType
{
    Text = 1,
    Chat = 2
}
