using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Options;

namespace BotSharp.Core.Rules.Criteria.Llm;

/// <summary>
/// Evaluates rule trigger criteria by asking an LLM whether the request meets a
/// natural-language condition. Runs the "criteria_check" template (which instructs
/// the model to answer "1" for met / "0" for not met) as the system prompt through the
/// instruct service, so the call is captured in the instruction log.
///
/// Note: LLM evaluation is non-deterministic and network-dependent. This evaluator is the
/// last resort the rule engine falls back to, so it never returns null: it fails closed
/// (returns false) on any error, empty response, or unparseable answer.
/// </summary>
public class LlmCriteriaEvaluator : IRuleCriteriaEvaluator
{
    private const string DefaultTemplateName = "criteria_check";

    private readonly IServiceProvider _services;
    private readonly ILogger<LlmCriteriaEvaluator> _logger;

    public LlmCriteriaEvaluator(
        IServiceProvider services,
        ILogger<LlmCriteriaEvaluator> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Type => BuiltInRuleCriteria.Llm;

    public async Task<bool?> EvaluateAsync(Agent agent, AgentRule agentRule, IRuleTrigger trigger, RuleCriteriaContext context)
    {
        var settings = context.Options.GetData<LlmCriteriaSettings>() ?? new();

        // The Rules agent hosts the criteria-check template by default.
        var agentId = !string.IsNullOrWhiteSpace(settings.AgentId) ? settings.AgentId! : BuiltInAgentId.RulesInterpreter;
        var templateName = !string.IsNullOrWhiteSpace(settings.TemplateName)
                        ? settings.TemplateName! : (agentId == BuiltInAgentId.RulesInterpreter ? DefaultTemplateName : $"{trigger.Name}_criteria");

        var input = BuildInput(agentRule.CriteriaConfig, settings);
        var msg = $"rule trigger ({trigger.Name}) llm criteria (agent {agentId}, template {templateName}).";

        try
        {
            var agentService = _services.GetRequiredService<IAgentService>();
            var innerAgent = await agentService.GetAgent(agentId);
            if (innerAgent == null)
            {
                _logger.LogWarning($"Unable to find agent for {msg}");
                return true;
            }

            // The criteria template is rendered off the request states, which are not yet
            // in the conversation state at evaluation time, so pass them as the render data.
            var template = innerAgent.Templates.FirstOrDefault(x => x.Name.IsEqualTo(templateName));
            if (template == null || string.IsNullOrWhiteSpace(template.Content))
            {
                _logger.LogWarning($"Unable to find agent template for {msg}");
                return true;
            }

            // "#TEMPLATE#" makes the instruct service use the rendered template as the system
            // instruction and the input as the user message.
            var instructService = _services.GetRequiredService<IInstructService>();
            var response = await instructService.Execute(
                agentId,
                new RoleDialogModel(AgentRole.User, input),
                instruction: "#TEMPLATE#",
                templateName: templateName,
                // The rule engine already gave the code evaluator its turn; keep this one llm-only.
                codeOptions: new CodeInstructOptions { Disabled = true },
                renderData: BuildRenderData(context));

            var answer = response?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(answer))
            {
                _logger.LogWarning($"Empty llm response for {msg}");
                return false;
            }

            var isTriggered = ParseResult(answer);
            _logger.Log(isTriggered ? LogLevel.Information : LogLevel.Warning,
                $"Llm criteria result ({answer}) => {isTriggered} for {msg}");
            return isTriggered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when handling {msg}");
            return false;
        }
    }

    private static Dictionary<string, object> BuildRenderData(RuleCriteriaContext context)
    {
        var data = new Dictionary<string, object>();
        if (context.States.IsNullOrEmpty())
        {
            return data;
        }

        foreach (var state in context.States!)
        {
            if (string.IsNullOrEmpty(state.Key))
            {
                continue;
            }

            data[state.Key] = state.Value;
        }

        return data;
    }

    private static string BuildInput(RuleCriteriaConfig? criteriaConfig, LlmCriteriaSettings settings)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(criteriaConfig?.Criteria))
        {
            sb.AppendLine("## Rule");
            sb.AppendLine(criteriaConfig.Criteria);
            sb.AppendLine();
        }

        var arguments = settings.ArgumentContent?.GetRawText();
        if (!string.IsNullOrWhiteSpace(arguments) && arguments != "{}")
        {
            sb.AppendLine("## Input");
            sb.AppendLine(arguments);
        }

        return sb.ToString().Trim();
    }

    private static bool ParseResult(string answer)
    {
        if (answer.IsEqualTo("1") || answer.IsEqualTo("true") || answer.IsEqualTo("yes"))
        {
            return true;
        }

        if (answer.IsEqualTo("0") || answer.IsEqualTo("false") || answer.IsEqualTo("no"))
        {
            return false;
        }

        // Fall back to the leading token; the template constrains output to "1"/"0".
        return answer.StartsWith("1", StringComparison.Ordinal);
    }
}
