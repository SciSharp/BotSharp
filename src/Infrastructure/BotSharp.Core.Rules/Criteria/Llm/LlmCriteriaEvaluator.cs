using BotSharp.Abstraction.Templating;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Core.Rules.Criteria.Llm;

/// <summary>
/// Evaluates rule trigger criteria by asking an LLM whether the request meets a
/// natural-language condition. Renders the "criteria_check" template (which instructs
/// the model to answer "1" for met / "0" for not met) as the system prompt and calls
/// the chat completion provider directly.
///
/// Note: LLM evaluation is non-deterministic and network-dependent. It fails closed
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

    public async Task<bool> EvaluateAsync(Agent agent, IRuleTrigger trigger, RuleCriteriaContext context)
    {
        var settings = context.Options.GetData<LlmCriteriaSettings>() ?? new();
        var rule = agent.Rules.FirstOrDefault(x => x.TriggerName.IsEqualTo(trigger.Name));

        // The Rules agent hosts the criteria-check template by default.
        var agentId = !string.IsNullOrWhiteSpace(settings.AgentId) ? settings.AgentId! : BuiltInAgentId.RulesInterpreter;
        var templateName = !string.IsNullOrWhiteSpace(settings.TemplateName) ? settings.TemplateName! : DefaultTemplateName;

        var input = BuildInput(rule?.Config, settings);
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

            // Render the template as the system instruction, exposing the request states.
            var render = _services.GetRequiredService<ITemplateRender>();
            var template = innerAgent.Templates.FirstOrDefault(x => x.Name.IsEqualTo(templateName));
            if (template == null || string.IsNullOrWhiteSpace(template.Content))
            {
                _logger.LogWarning($"Unable to find agent template for {msg}");
                return true;
            }

            var instruction = render.Render(template.Content, BuildRenderData(context));

            // Prefer the template's own LLM config when it is fully specified.
            var llmConfig = innerAgent.LlmConfig;
            if (template.LlmConfig?.IsValid == true)
            {
                llmConfig = new AgentLlmConfig(template.LlmConfig);
            }

            var completer = CompletionProvider.GetChatCompletion(_services, agentConfig: llmConfig);
            if (completer == null)
            {
                _logger.LogWarning($"Unable to resolve chat completion provider for {msg}");
                return false;
            }

            var response = await completer.GetChatCompletions(new Agent
            {
                Id = innerAgent.Id,
                Name = innerAgent.Name,
                Instruction = instruction,
                LlmConfig = llmConfig
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, input)
            });

            var answer = response?.Content?.Trim() ?? string.Empty;
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

    private static string BuildInput(RuleConfig? ruleConfig, LlmCriteriaSettings settings)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(ruleConfig?.Criteria))
        {
            sb.AppendLine("## Rule");
            sb.AppendLine(ruleConfig.Criteria);
            sb.AppendLine();
        }

        var arguments = settings.ArgumentContent?.RootElement.GetRawText();
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
