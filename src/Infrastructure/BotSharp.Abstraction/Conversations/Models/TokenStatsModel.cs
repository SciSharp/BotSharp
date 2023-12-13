namespace BotSharp.Abstraction.Conversations.Models;

public class TokenStatsModel
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public string Prompt { get; set; }
    public int PromptCount { get; set; }
    public int CompletionCount { get; set; }
    public AgentLlmConfig LlmConfig { get; set; }
}
