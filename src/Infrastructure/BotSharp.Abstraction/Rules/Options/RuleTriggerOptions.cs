using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Rules.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// How long one rule may take before it is given up on. Null - the default - means no per-rule limit,
    /// so only the caller's own cancellation token stops anything, exactly as before this existed.
    /// </summary>
    /// <remarks>
    /// Scoped to a single rule on purpose: the rule that runs out of time is the only one abandoned, and
    /// the rules behind it still get their turn. That is what separates it from the caller's token, which
    /// stops the whole run.
    /// Cancellation is cooperative, so this bounds only the parts of a rule that observe a token. A rule
    /// wedged inside its criteria evaluation or its agent turn is not interrupted by it - neither call
    /// takes a token - and the loop waits for that rule regardless. A value of zero or less is read as no
    /// limit, so <c>Timeout.InfiniteTimeSpan</c> says the same thing as null.
    /// </remarks>
    public TimeSpan? RuleTimeout { get; set; }

    /// <summary>
    /// Called with a conversation id the moment that conversation is created, before its message is sent.
    /// </summary>
    /// <remarks>
    /// What <c>Triggered</c> returns is only the rules that ran to completion, so a rule that throws after
    /// its conversation was started - or a run that is cancelled - leaves behind a conversation the caller
    /// never hears about. This is how a caller that has to account for every conversation, rather than only
    /// the successful ones, is told about them.
    /// Awaited before the conversation's message is sent, so a caller that records the id has finished
    /// recording it by the time anything that can fail runs. Rules run one after another, so it is never
    /// invoked concurrently. A callback that throws is swallowed: reporting the id is not worth costing the
    /// rule its run.
    /// JsonIgnore because this type is also the body of the rule trigger API request: a delegate has no
    /// wire representation, and without this the serializer refuses the whole request model, not just this
    /// property. An in-process caller is the only one that can set it, which is the only one it is for.
    /// </remarks>
    [JsonIgnore]
    public Func<string, Task>? OnConversationCreated { get; set; }
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