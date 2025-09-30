using BotSharp.Abstraction.Models;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Services;

public class PyInterpretService : ICodeInterpretService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PyInterpretService> _logger;

    public PyInterpretService(
        IServiceProvider services,
        ILogger<PyInterpretService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => "python-interpreter";

    public async Task<CodeInterpretResult> RunCode(string code, List<KeyValue>? arguments = null, CodeInterpretOptions? options = null)
    {
        try
        {
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
                if (code.Contains("__main__") == true)
                {
                    globals.SetItem("__name__", new PyString("__main__"));
                }

                // Set arguments
                if (!arguments.IsNullOrEmpty())
                {
                    sys.argv = new PyList();
                    sys.argv.Append("code.py");

                    foreach (var arg in arguments)
                    {
                        sys.argv.Append($"--{arg.Key}");
                        sys.argv.Append($"{arg.Value}");
                    }
                }

                // Execute Python script
                PythonEngine.Exec(code, globals);

                // Get result
                var result = stringIO.getvalue().ToString();

                // Restore the original stdout/stderr
                sys.stdout = sys.__stdout__;
                sys.stderr = sys.__stderr__;

                return new CodeInterpretResult
                {
                    Result = result,
                    Success = true
                };
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error when executing python code in {nameof(PyInterpretService)}-{Provider}. {ex.Message}";
            _logger.LogError(ex, errorMsg);

            return new CodeInterpretResult
            {
                Success = false,
                ErrorMsg = errorMsg
            };
        }
    }
}
