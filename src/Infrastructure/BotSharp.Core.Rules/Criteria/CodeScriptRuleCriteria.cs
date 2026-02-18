using System.Text.Json;

namespace BotSharp.Core.Rules.Criteria;

public class CodeScriptRuleCriteria : IRuleCriteria
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CodeScriptRuleCriteria> _logger;
    private readonly CodingSettings _codingSettings;

    public CodeScriptRuleCriteria(
        IServiceProvider services,
        ILogger<CodeScriptRuleCriteria> logger,
        CodingSettings codingSettings)
    {
        _services = services;
        _logger = logger;
        _codingSettings = codingSettings;
    }

    public string Provider => RuleConstant.DEFAULT_CRITERIA_PROVIDER;

    public async Task<RuleCriteriaResult> ValidateAsync(Agent agent, IRuleTrigger trigger, RuleCriteriaContext context)
    {
        var result = new RuleCriteriaResult();

        if (string.IsNullOrWhiteSpace(agent?.Id))
        {
            return result;
        }

        var provider = context.Parameters.GetValueOrDefault("code_processor", BuiltInCodeProcessor.PyInterpreter);
        var processor = _services.GetServices<ICodeProcessor>().FirstOrDefault(x => x.Provider.IsEqualTo(provider));
        if (processor == null)
        {
            _logger.LogWarning($"Unable to find code processor: {provider}.");
            return result;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var scriptName = context.Parameters.GetValueOrDefault("code_script_name", $"{trigger.Name}_rule.py");
        var codeScript = await agentService.GetAgentCodeScript(agent.Id, scriptName, scriptType: AgentCodeScriptType.Src);

        var msg = $"rule trigger ({trigger.Name}) code script ({scriptName}) in agent ({agent.Name}).";

        if (codeScript == null || string.IsNullOrWhiteSpace(codeScript.Content))
        {
            _logger.LogWarning($"Unable to find {msg}.");
            return result;
        }

        try
        {
            var hooks = _services.GetHooks<IInstructHook>(agent.Id);

            var argName = context.Parameters.GetValueOrDefault("code_script_arg_name", null);
            var argValue = context.Parameters.TryGetValue("code_script_arg_value", out var val) && val != null ? JsonSerializer.Deserialize<JsonElement>(val) : (JsonElement?)null;
            var arguments = BuildArguments(argName, argValue);
            var codeExecutionContext = new CodeExecutionContext
            {
                CodeScript = codeScript,
                Arguments = arguments,
                InvokeFrom = nameof(CodeScriptRuleCriteria)
            };

            foreach (var hook in hooks)
            {
                await hook.BeforeCodeExecution(agent, codeExecutionContext);
            }

            codeScript = codeExecutionContext.CodeScript;
            var (useLock, useProcess, timeoutSeconds) = CodingUtil.GetCodeExecutionConfig(_codingSettings);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = processor.Run(codeScript.Content, options: new()
            {
                ScriptName = scriptName,
                Arguments = codeExecutionContext.Arguments,
                UseLock = useLock,
                UseProcess = useProcess
            }, cancellationToken: cts.Token);

            var codeResponse = new CodeExecutionResponseModel
            {
                CodeProcessor = processor.Provider,
                CodeScript = codeScript,
                Arguments = arguments.DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value ?? string.Empty),
                ExecutionResult = response
            };

            foreach (var hook in hooks)
            {
                await hook.AfterCodeExecution(agent, codeExecutionContext, codeResponse);
            }

            if (response == null || !response.Success)
            {
                _logger.LogWarning($"Failed to handle {msg}");
                return result;
            }

            LogLevel logLevel;
            if (response.Result.IsEqualTo("true"))
            {
                logLevel = LogLevel.Information;
                result.Success = true;
                result.IsValid = true;
            }
            else
            {
                logLevel = LogLevel.Warning;
            }

            _logger.Log(logLevel, $"Code script execution result ({response}) from {msg}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when handling {msg}");
            return result;
        }
    }

    private List<KeyValue> BuildArguments(string? name, JsonElement? args)
    {
        var keyValues = new List<KeyValue>();
        if (args != null)
        {
            keyValues.Add(new KeyValue(name ?? "trigger_args", args.Value.GetRawText()));
        }
        return keyValues;
    }
}
