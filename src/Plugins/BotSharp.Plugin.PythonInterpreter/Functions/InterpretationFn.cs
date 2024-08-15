using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Interpreters.Models;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Functions;

public class InterpretationFn : IFunctionCallback
{
    public string Name => "python_interpreter";
    public string Indication => "Interpreting python code";

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<InterpretationRequest>(message.FunctionArgs);

        using (Py.GIL())
        {
            // Import necessary Python modules
            dynamic sys = Py.Import("sys");
            dynamic io = Py.Import("io");

            // Redirect standard output to capture it
            dynamic stringIO = io.StringIO();
            sys.stdout = stringIO;

            // Execute a simple Python script
            using var locals = new PyDict();
            PythonEngine.Exec(args.Script, null, locals);

            // Console.WriteLine($"Result from Python: {result}");
            message.Content = stringIO.getvalue();

            // Restore the original stdout
            sys.stdout = sys.__stdout__;
        }

        return true;
    }
}
