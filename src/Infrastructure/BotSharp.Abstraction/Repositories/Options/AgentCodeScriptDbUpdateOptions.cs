namespace BotSharp.Abstraction.Repositories.Options;

public class AgentCodeScriptDbUpdateOptions
{
    [JsonPropertyName("is_upsert")]
    public bool IsUpsert { get; set; }
}
