namespace BotSharp.Abstraction.Conversations.Models;

public class TokenStatsModel
{
    public string Model { get; set; }
    public string Prompt { get; set; }
    public int PromptCount { get; set; }
    public int CompletionCount { get; set; }

    /// <summary>
    /// Prompt cost per 1K token
    /// </summary>
    public float PromptCost { get; set; }

    /// <summary>
    /// Completion cost per 1K token
    /// </summary>
    public float CompletionCost { get; set; }
}
