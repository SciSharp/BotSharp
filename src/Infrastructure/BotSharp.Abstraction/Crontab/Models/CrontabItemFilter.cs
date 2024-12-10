namespace BotSharp.Abstraction.Crontab.Models;

public class CrontabItemFilter : Pagination
{
    [JsonPropertyName("user_ids")]
    public IEnumerable<string>? UserIds { get; set; }

    [JsonPropertyName("agent_ids")]
    public IEnumerable<string>? AgentIds { get; set; }

    [JsonPropertyName("conversation_ids")]
    public IEnumerable<string>? ConversationIds { get; set; }

    [JsonPropertyName("titles")]
    public IEnumerable<string>? Titles { get; set; }

    public CrontabItemFilter()
    {
        
    }

    public static CrontabItemFilter Empty()
    {
        return new CrontabItemFilter();
    }
}
