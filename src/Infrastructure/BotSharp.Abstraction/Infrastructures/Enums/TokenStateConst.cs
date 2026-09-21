namespace BotSharp.Abstraction.Infrastructures.Enums;

/// <summary>
/// Conversation states accumulating token usage and cost of the current conversation.
/// </summary>
public static class TokenStateConst
{
    public const string PROMPT_TOTAL = "prompt_total";
    public const string COMPLETION_TOTAL = "completion_total";
    public const string LLM_TOTAL_COST = "llm_total_cost";
}
