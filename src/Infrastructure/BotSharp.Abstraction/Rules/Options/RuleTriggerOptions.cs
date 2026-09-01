using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Rules.Constants;
using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
{
    /// <summary>
    /// Filter agents
    /// </summary>
    public AgentFilter? AgentFilter { get; set; }

    /// <summary>
    /// Criteria
    /// </summary>
    public CriteriaOptions? Criteria { get; set; }

    /// <summary>
    /// How long to pause after sending a message to a triggered agent, before moving on
    /// to the next rule. Keeps a burst of triggered rules from hammering the LLM provider
    /// all at once. Set to zero to disable the pause.
    /// </summary>
    public int SendMessageDelayMs { get; set; } = DefaultSendMessageDelayMs;

    public const int DefaultSendMessageDelayMs = 200;

    /// <summary>
    /// How many triggered rules may run at the same time. Each one still gets its own
    /// service scope, so concurrent runs do not share conversation, state or routing
    /// services. Set to one to run them sequentially. Null falls back to
    /// <c>RuleSettings.MaxConcurrency</c>, then to <see cref="DefaultMaxConcurrency"/>.
    /// </summary>
    public int? MaxConcurrency { get; set; }

    public const int DefaultMaxConcurrency = 5;
}

public class CriteriaOptions
{
    /// <summary>
    /// How the criteria is evaluated (see <see cref="BuiltInRuleCriteria"/>).
    /// Selects which <c>IRuleCriteriaEvaluator</c> handles this criteria.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Evaluator-specific settings, kept as raw JSON so each evaluator can
    /// deserialize it into its own strongly-typed settings model.
    /// Use <see cref="GetData{T}"/> to read it.
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Deserialize <see cref="Data"/> into an evaluator-specific settings type.
    /// Returns default (null) when no data is provided.
    /// </summary>
    public T? GetData<T>(JsonSerializerOptions? options = null)
    {
        if (Data == null || Data.Value.ValueKind == JsonValueKind.Null || Data.Value.ValueKind == JsonValueKind.Undefined)
        {
            return default;
        }

        return Data.Value.Deserialize<T>(options ?? _webJsonOptions);
    }

    private static readonly JsonSerializerOptions _webJsonOptions = new(JsonSerializerDefaults.Web);
}