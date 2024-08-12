using BotSharp.Plugin.PythonInterpreter.Hooks;

namespace BotSharp.Plugin.PythonInterpreter;

public class InterpreterPlugin : IBotSharpPlugin
{
    public string Id => "23174e08-e866-4173-824a-cf1d97afa8d0";
    public string Name => "Python Interpreter";
    public string Description => "Python Interpreter enables AI to write and execute Python code within a secure, sandboxed environment.";
    public string? IconUrl => "https://static.vecteezy.com/system/resources/previews/012/697/295/non_2x/3d-python-programming-language-logo-free-png.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IAgentHook, InterpreterAgentHook>();
        services.AddScoped<IAgentUtilityHook, InterpreterUtilityHook>();
    }
}
