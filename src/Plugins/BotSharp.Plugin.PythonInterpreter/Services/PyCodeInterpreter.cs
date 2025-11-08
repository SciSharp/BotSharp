using BotSharp.Abstraction.Coding.Models;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Services;

public class PyCodeInterpreter : ICodeProcessor
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PyCodeInterpreter> _logger;
    private readonly CodeScriptExecutor _executor;

    public PyCodeInterpreter(
        IServiceProvider services,
        ILogger<PyCodeInterpreter> logger,
        CodeScriptExecutor executor)
    {
        _services = services;
        _logger = logger;
        _executor = executor;
    }

    public string Provider => "botsharp-py-interpreter";

    public async Task<CodeInterpretResponse> RunAsync(string codeScript, CodeInterpretOptions? options = null)
    {
        var cancellationToken = options?.CancellationToken ?? CancellationToken.None;

        if (options?.UseMutex == true)
        {
            return await _executor.ExecuteAsync(async () =>
            {
                return InnerRunCode(codeScript, options);
            }, cancellationToken: cancellationToken);
        }

        return InnerRunCode(codeScript, options);
    }

    public async Task<CodeGenerationResult> GenerateCodeScriptAsync(string text, CodeGenerationOptions? options = null)
    {
        Agent? agent = null;

        var agentId = options?.AgentId;
        var templateName = options?.TemplateName;

        var agentService = _services.GetRequiredService<IAgentService>();
        if (!string.IsNullOrEmpty(agentId))
        {
            agent = await agentService.GetAgent(agentId);
        }
        
        var instruction = string.Empty;
        if (agent != null && !string.IsNullOrEmpty(templateName))
        {
            instruction = agent.Templates?.FirstOrDefault(x => x.Name.IsEqualTo(templateName))?.Content;
        }

        var innerAgent = new Agent
        {
            Id = agent?.Id ?? BuiltInAgentId.AIProgrammer,
            Name = agent?.Name ?? "AI Programmer",
            Instruction = instruction,
            LlmConfig = new AgentLlmConfig
            {
                Provider = options?.Provider ?? "openai",
                Model = options?.Model ?? "gpt-5-mini",
                MaxOutputTokens = options?.MaxOutputTokens,
                ReasoningEffortLevel = options?.ReasoningEffortLevel
            },
            TemplateDict = options?.Data ?? new()
        };
        
        text = text.IfNullOrEmptyAs("Please follow the instruction to generate code script.")!;
        var completion = CompletionProvider.GetChatCompletion(_services, provider: innerAgent.LlmConfig.Provider, model: innerAgent.LlmConfig.Model);
        var response = await completion.GetChatCompletions(innerAgent, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, text)
            {
                CurrentAgentId = innerAgent.Id
            }
        });

        return new CodeGenerationResult
        {
            Success = true,
            Content = response.Content,
            Language = options?.Language ?? "python"
        };
    }


    #region Private methods
    private CodeInterpretResponse InnerRunCode(string codeScript, CodeInterpretOptions? options = null)
    {
        _logger.LogWarning($"Begin running python code script in {Provider}: ${options?.ScriptName ?? codeScript.SubstringMax(30)}");

        var response = CoreRun(codeScript, options);

        _logger.LogWarning($"End running python code script in {Provider}: ${options?.ScriptName ?? codeScript.SubstringMax(30)}");

        return response;
    }

    private CodeInterpretResponse CoreRun(string codeScript, CodeInterpretOptions? options = null)
    {
        _logger.LogWarning($"Begin {nameof(CoreRun)} in {Provider}: ${options?.ScriptName}");

        var token = options?.CancellationToken ?? CancellationToken.None;
        token.ThrowIfCancellationRequested();

        using (Py.GIL())
        {
            // Import necessary Python modules
            dynamic sys = Py.Import("sys");
            dynamic io = Py.Import("io");

            // Redirect standard output/error to capture it
            dynamic stringIO = io.StringIO();
            sys.stdout = stringIO;
            sys.stderr = stringIO;

            // Set global items
            using var globals = new PyDict();
            if (codeScript.Contains("__main__") == true)
            {
                globals.SetItem("__name__", new PyString("__main__"));
            }

            // Set arguments
            var list = new PyList();
            if (options?.Arguments?.Any() == true)
            {
                list.Append(new PyString(options?.ScriptName ?? "script.py"));

                foreach (var arg in options!.Arguments)
                {
                    if (!string.IsNullOrWhiteSpace(arg.Key) && !string.IsNullOrWhiteSpace(arg.Value))
                    {
                        list.Append(new PyString($"--{arg.Key}"));
                        list.Append(new PyString($"{arg.Value}"));
                    }
                }
            }
            sys.argv = list;

            token.ThrowIfCancellationRequested();

            // Execute Python script
            PythonEngine.Exec(codeScript, globals);

            // Get result
            var result = stringIO.getvalue()?.ToString() as string;

            token.ThrowIfCancellationRequested();
            _logger.LogWarning($"End {nameof(CoreRun)} in {Provider}: ${options?.ScriptName}");

            // Restore the original stdout/stderr/argv
            sys.stdout = sys.__stdout__;
            sys.stderr = sys.__stderr__;
            sys.argv = new PyList();

            return new CodeInterpretResponse
            {
                Result = result?.TrimEnd('\r', '\n'),
                Success = true
            };
        }
    }
    #endregion
}
