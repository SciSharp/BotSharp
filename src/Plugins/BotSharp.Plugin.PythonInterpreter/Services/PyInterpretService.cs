using BotSharp.Core.CodeInterpreter;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Services;

public class PyInterpretService : ICodeInterpretService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PyInterpretService> _logger;
    private readonly CodeScriptExecutor _executor;

    public PyInterpretService(
        IServiceProvider services,
        ILogger<PyInterpretService> logger,
        CodeScriptExecutor executor)
    {
        _services = services;
        _logger = logger;
        _executor = executor;
    }

    public string Provider => "botsharp-py-interpreter";

    public async Task<CodeInterpretResult> RunCode(string codeScript, CodeInterpretOptions? options = null)
    {
        if (options?.UseMutex == true)
        {
            return await _executor.Execute(async () =>
            {
                return InnerRunCode(codeScript, options);
            }, cancellationToken: options?.CancellationToken ?? CancellationToken.None);
        }
        return InnerRunCode(codeScript, options);
    }

    private CodeInterpretResult InnerRunCode(string codeScript, CodeInterpretOptions? options = null)
    {
        try
        {
            return CoreRun(codeScript, options);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error when executing inner python code in {nameof(PyInterpretService)}: {Provider}.";
            _logger.LogError(ex, errorMsg);

            return new CodeInterpretResult
            {
                Success = false,
                ErrorMsg = errorMsg
            };
        }
    }

    private CodeInterpretResult CoreRun(string codeScript, CodeInterpretOptions? options = null)
    {
        using (Py.GIL())
        {
            // Import necessary Python modules
            dynamic sys = Py.Import("sys");
            dynamic io = Py.Import("io");

            try
            {
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

                    foreach (var arg in options.Arguments)
                    {
                        if (!string.IsNullOrWhiteSpace(arg.Key) && !string.IsNullOrWhiteSpace(arg.Value))
                        {
                            list.Append(new PyString($"--{arg.Key}"));
                            list.Append(new PyString($"{arg.Value}"));
                        }
                    }
                }
                sys.argv = list;

                // Execute Python script
                PythonEngine.Exec(codeScript, globals);

                // Get result
                var result = stringIO.getvalue()?.ToString() as string;

                return new CodeInterpretResult
                {
                    Result = result?.TrimEnd('\r', '\n'),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error when executing core python code in {nameof(PyInterpretService)}: {Provider}. {ex.Message}";
                _logger.LogError(ex, errorMsg);

                return new CodeInterpretResult
                {
                    Success = false,
                    ErrorMsg = errorMsg
                };
            }
            finally
            {
                // Restore the original stdout/stderr/argv
                sys.stdout = sys.__stdout__;
                sys.stderr = sys.__stderr__;
                sys.argv = new PyList();
            }
        }
    }
}
