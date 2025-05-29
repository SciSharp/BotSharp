using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Models;

public class ConversationSummaryModel
{
    [JsonPropertyName("conversation_ids")]
    public IEnumerable<string> ConversationIds { get; set; } = new List<string>();

    private string _agentId;

    [JsonPropertyName("agent_id")]
    public string AgentId
    {
        get => _agentId ?? BuiltInAgentId.AIAssistant;
        set => _agentId = value;
    }

    private string _templateName;

    [JsonPropertyName("template_name")]
    public string TemplateName
    { 
        get => _templateName ?? "conversation.summary";
        set => _templateName = value;
    }
}
