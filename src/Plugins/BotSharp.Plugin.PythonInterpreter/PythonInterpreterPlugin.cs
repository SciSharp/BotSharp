using BotSharp.Plugin.PythonInterpreter.Hooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Python.Runtime;
using System.IO;

namespace BotSharp.Plugin.PythonInterpreter;

public class PythonInterpreterPlugin : IBotSharpAppPlugin
{
    public string Id => "23174e08-e866-4173-824a-cf1d97afa8d0";
    public string Name => "Python Interpreter";
    public string Description => "Python Interpreter enables AI to write and execute Python code within a secure, sandboxed environment.";
    public string? IconUrl => "https://static.vecteezy.com/system/resources/previews/012/697/295/non_2x/3d-python-programming-language-logo-free-png.png";

    private nint _pyState;

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new PythonInterpreterSettings();
        config.Bind("PythonInterpreter", settings);
        services.AddSingleton(x => settings);

        services.AddScoped<IAgentUtilityHook, PyProgrammerUtilityHook>();
    }

    public void Configure(IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<PythonInterpreterSettings>();

        // For Python interpreter plugin
        if (File.Exists(settings.DllLocation))
        {
            Runtime.PythonDLL = settings.DllLocation;
            PythonEngine.Initialize();
            _pyState = PythonEngine.BeginAllowThreads();

            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() => {
                PythonEngine.EndAllowThreads(_pyState);
                PythonEngine.Shutdown();
            });
        }
        else
        {
            Serilog.Log.Error($"Python DLL found at {settings.DllLocation}");
        }
    }
}
