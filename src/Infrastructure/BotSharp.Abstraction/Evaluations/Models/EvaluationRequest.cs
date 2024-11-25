using BotSharp.Abstraction.Processors.Models;

namespace BotSharp.Abstraction.Evaluations.Models;

public class EvaluationRequest : LlmBaseRequest
{
    [JsonPropertyName("agent_id")]
    public new string AgentId { get; set; }

    [JsonPropertyName("states")]
    public IEnumerable<MessageState> States { get; set; } = [];

    [JsonPropertyName("chat")]
    public ChatEvaluationRequest Chat { get; set; } = new ChatEvaluationRequest();

    [JsonPropertyName("metric")]
    public MetricEvaluationRequest Metric { get; set; } = new MetricEvaluationRequest();
}


public class ChatEvaluationRequest
{
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

    public ChatEvaluationRequest()
    {
        
    }
}


public class MetricEvaluationRequest
{
    [JsonPropertyName("additional_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AdditionalInstruction { get; set; }

    [JsonPropertyName("metrics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<NameDesc>? Metrics { get; set; } = [];

    public MetricEvaluationRequest()
    {
        
    }
}