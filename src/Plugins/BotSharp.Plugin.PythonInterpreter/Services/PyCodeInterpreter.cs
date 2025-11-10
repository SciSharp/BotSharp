using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Services;

public class PyCodeInterpreter : ICodeProcessor
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PyCodeInterpreter> _logger;
    private readonly CodeScriptExecutor _executor;
    private readonly CodingSettings _settings;

    public PyCodeInterpreter(
        IServiceProvider services,
        ILogger<PyCodeInterpreter> logger,
        CodeScriptExecutor executor,
        CodingSettings settings)
    {
        _services = services;
        _logger = logger;
        _executor = executor;
        _settings = settings;
    }

    public string Provider => BuiltInCodeProcessor.PyInterpreter;

    public async Task<CodeInterpretResponse> RunAsync(string codeScript, CodeInterpretOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (options?.UseLock == true)
        {
            try
            {
                return await _executor.ExecuteAsync(async () =>
                {
                    return await InnerRunCode(codeScript, options, cancellationToken);
                }, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when using code script executor.");
                return new() { ErrorMsg = ex.Message };
            }
        }
        
        return await InnerRunCode(codeScript, options, cancellationToken);
    }

    public async Task<CodeGenerationResult> GenerateCodeScriptAsync(string text, CodeGenerationOptions? options = null)
    {
        Agent? agent = null;

        var agentId = options?.AgentId;
        var templateName = options?.TemplateName;

        if (!string.IsNullOrEmpty(agentId))
        {
            var agentService = _services.GetRequiredService<IAgentService>();
            agent = await agentService.GetAgent(agentId);
        }
        
        var instruction = string.Empty;
        if (agent != null && !string.IsNullOrEmpty(templateName))
        {
            instruction = agent.Templates?.FirstOrDefault(x => x.Name.IsEqualTo(templateName))?.Content;
        }

        var (provider, model) = GetLlmProviderModel();
        var innerAgent = new Agent
        {
            Id = agent?.Id ?? BuiltInAgentId.AIProgrammer,
            Name = agent?.Name ?? "AI Programmer",
            Instruction = instruction,
            LlmConfig = new AgentLlmConfig
            {
                Provider = options?.Provider ?? provider,
                Model = options?.Model ?? model,
                MaxOutputTokens = options?.MaxOutputTokens ?? _settings?.CodeGeneration?.MaxOutputTokens,
                ReasoningEffortLevel = options?.ReasoningEffortLevel ?? _settings?.CodeGeneration?.ReasoningEffortLevel
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
            Language = options?.ProgrammingLanguage ?? "python"
        };
    }


    #region Private methods
    private async Task<CodeInterpretResponse> InnerRunCode(string codeScript, CodeInterpretOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = new CodeInterpretResponse();
        var scriptName = options?.ScriptName ?? codeScript.SubstringMax(30);

        try
        {
            _logger.LogWarning($"Begin running python code script in {Provider}: {scriptName}");

            if (options?.UseProcess == true)
            {
                response = await CoreRunProcess(codeScript, options, cancellationToken);
            }
            else
            {
                response = await CoreRunScript(codeScript, options, cancellationToken);
            }
            
            _logger.LogWarning($"End running python code script in {Provider}: {scriptName}");

            return response;
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogError(oce, $"Operation cancelled in {nameof(InnerRunCode)} in {Provider}.");
            response.ErrorMsg = oce.Message;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when executing code script ({scriptName}) in {nameof(InnerRunCode)} in {Provider}.");
            response.ErrorMsg = ex.Message;
            return response;
        }
    }

    private async Task<CodeInterpretResponse> CoreRunScript(string codeScript, CodeInterpretOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var execTask = Task.Factory.StartNew(() =>
        {
            // For observation purpose
            var requestId = Guid.NewGuid();
            _logger.LogWarning($"Before acquiring Py.GIL for request {requestId}");

            using (Py.GIL())
            {
                _logger.LogWarning($"After acquiring Py.GIL for request {requestId}");

                // Import necessary Python modules
                dynamic sys = Py.Import("sys");
                dynamic io = Py.Import("io");

                try
                {
                    // Redirect standard output/error to capture it
                    dynamic outIO = io.StringIO();
                    dynamic errIO = io.StringIO();
                    sys.stdout = outIO;
                    sys.stderr = errIO;

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
                        list.Append(new PyString(options?.ScriptName ?? $"{Guid.NewGuid()}.py"));

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

                    cancellationToken.ThrowIfCancellationRequested();

                    // Execute Python script
                    PythonEngine.Exec(codeScript, globals);

                    _logger.LogWarning($"Complete {nameof(CoreRunScript)} in {Provider}: ${options?.ScriptName}");

                    // Get result
                    var stdout = outIO.getvalue()?.ToString() as string;
                    var stderr = errIO.getvalue()?.ToString() as string;

                    cancellationToken.ThrowIfCancellationRequested();

                    return new CodeInterpretResponse
                    {
                        Result = stdout?.TrimEnd('\r', '\n') ?? string.Empty,
                        Success = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in {nameof(CoreRunScript)} in {Provider}.");
                    return new() { ErrorMsg = ex.Message };
                }
                finally
                {
                    // Restore the original stdout/stderr/argv
                    sys.stdout = sys.__stdout__;
                    sys.stderr = sys.__stderr__;
                    sys.argv = new PyList();
                }
            };
        }, cancellationToken);

        return await execTask.WaitAsync(cancellationToken);
    }


    private async Task<CodeInterpretResponse> CoreRunProcess(string codeScript, CodeInterpretOptions? options = null, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // Add raw code script
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(codeScript);

        // Add arguments (safe—no shared state)
        if (options?.Arguments?.Any() == true)
        {
            foreach (var arg in options.Arguments!)
            {
                if (!string.IsNullOrWhiteSpace(arg.Key) && !string.IsNullOrWhiteSpace(arg.Value))
                {
                    psi.ArgumentList.Add($"--{arg.Key}");
                    psi.ArgumentList.Add($"{arg.Value}");
                }
            }
        }

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
        {
            throw new InvalidOperationException($"Failed to start Python process in {Provider}.");
        }

        try
        {
            using var reg = cancellationToken.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                }
                catch { }
            });

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll([proc.WaitForExitAsync(cancellationToken), stdoutTask, stderrTask]);

            cancellationToken.ThrowIfCancellationRequested();

            return new CodeInterpretResponse
            {
                Success = proc.ExitCode == 0,
                Result = stdoutTask.Result?.TrimEnd('\r', '\n') ?? string.Empty,
                ErrorMsg = stderrTask.Result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in {nameof(CoreRunProcess)} in {Provider}.");
            throw;
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit();
                }
            }
            catch { }
        }
    }

    private (string, string) GetLlmProviderModel()
    {
        var provider = _settings.CodeGeneration?.Provider;
        var model = _settings.CodeGeneration?.Model;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-5-mini";

        return (provider, model);
    }
    #endregion
}
