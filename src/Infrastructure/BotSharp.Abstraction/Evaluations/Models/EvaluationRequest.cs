using BotSharp.Abstraction.Processors.Models;

namespace BotSharp.Abstraction.Evaluations.Models;

public class EvaluationRequest : LlmBaseRequest
{
    [JsonPropertyName("agent_id")]
    public new string AgentId { get; set; }

    [JsonPropertyName("states")]
    public IEnumerable<MessageState> States { get; set; } = [];

    [JsonPropertyName("duplicate_limit")]
    public int DuplicateLimit { get; set; } = 2;

    [JsonPropertyName("max_rounds")]
    public int MaxRounds { get; set; } = 20;


    [JsonPropertyName("additional_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AdditionalInstruction { get; set; }

    [JsonPropertyName("stop_criteria")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StopCriteria { get; set; }
}
