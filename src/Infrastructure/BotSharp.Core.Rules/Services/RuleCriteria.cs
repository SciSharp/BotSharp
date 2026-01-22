using System.Text.Json;

namespace BotSharp.Core.Rules.Services;

public class RuleCriteria : IRuleCriteria
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RuleCriteria> _logger;
    private readonly CodingSettings _codingSettings;

    public RuleCriteria(
        IServiceProvider services,
        ILogger<RuleCriteria> logger,
        CodingSettings codingSettings)
    {
        _services = services;
        _logger = logger;
        _codingSettings = codingSettings;
    }

    public string Provider => "botsharp-rule-criteria";

    public async Task<bool> ValidateAsync(Agent agent, IRuleTrigger trigger, CriteriaExecuteOptions options)
    {
        if (string.IsNullOrWhiteSpace(agent?.Id))
        {
            return false;
        }

        var provider = options.CodeProcessor ?? BuiltInCodeProcessor.PyInterpreter;
        var processor = _services.GetServices<ICodeProcessor>().FirstOrDefault(x => x.Provider.IsEqualTo(provider));
        if (processor == null)
        {
            _logger.LogWarning($"Unable to find code processor: {provider}.");
            return false;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var scriptName = options.CodeScriptName ?? $"{trigger.Name}_rule.py";
        var codeScript = await agentService.GetAgentCodeScript(agent.Id, scriptName, scriptType: AgentCodeScriptType.Src);

        var msg = $"rule trigger ({trigger.Name}) code script ({scriptName}) in agent ({agent.Name}) => args: {options.ArgumentContent?.RootElement.GetRawText()}.";

        if (codeScript == null || string.IsNullOrWhiteSpace(codeScript.Content))
        {
            _logger.LogWarning($"Unable to find {msg}.");
            return false;
        }

        try
        {
            var hooks = _services.GetHooks<IInstructHook>(agent.Id);

            var arguments = BuildArguments(options.ArgumentName, options.ArgumentContent);
            var context = new CodeExecutionContext
            {
                CodeScript = codeScript,
                Arguments = arguments
            };

            foreach (var hook in hooks)
            {
                await hook.BeforeCodeExecution(agent, context);
            }

            var (useLock, useProcess, timeoutSeconds) = CodingUtil.GetCodeExecutionConfig(_codingSettings);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = processor.Run(codeScript.Content, options: new()
            {
                ScriptName = scriptName,
                Arguments = arguments,
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
                await hook.AfterCodeExecution(agent, codeResponse);
            }

            if (response == null || !response.Success)
            {
                _logger.LogWarning($"Failed to handle {msg}");
                return false;
            }

            bool result;
            LogLevel logLevel;
            if (response.Result.IsEqualTo("true"))
            {
                logLevel = LogLevel.Information;
                result = true;
            }
            else
            {
                logLevel = LogLevel.Warning;
                result = false;
            }

            _logger.Log(logLevel, $"Code script execution result ({response}) from {msg}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when handling {msg}");
            return false;
        }
    }

    private List<KeyValue> BuildArguments(string? name, JsonDocument? args)
    {
        var keyValues = new List<KeyValue>();
        if (args != null)
        {
            keyValues.Add(new KeyValue(name ?? "trigger_args", args.RootElement.GetRawText()));
        }
        return keyValues;
    }
}
