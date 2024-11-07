using BotSharp.Abstraction.Processors.Models;

namespace BotSharp.Abstraction.Evaluations.Models;

public class EvaluationRequest : LlmBaseRequest
{
    [JsonPropertyName("agent_id")]
    public new string AgentId { get; set; }

    [JsonPropertyName("states")]
    public IEnumerable<MessageState> States { get; set; } = [];

    [JsonPropertyName("max_rounds")]
    public int MaxRounds { get; set; } = 20;

    [JsonPropertyName("ref_conversation_id")]
    public string RefConversationId { get; set; } = null!;
}
