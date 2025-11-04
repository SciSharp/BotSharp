using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        services.AddScoped<ICodeProcessor, PyCodeInterpreter>();
    }

    public void Configure(IApplicationBuilder app)
    {
        var sp = app.ApplicationServices;
        var settings = sp.GetRequiredService<PythonInterpreterSettings>();
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var logger = sp.GetRequiredService<ILogger<PyProgrammerFn>>();
        var pyLoc = settings.InstallLocation;

        try
        {
            if (File.Exists(pyLoc))
            {
                Runtime.PythonDLL = pyLoc;
                PythonEngine.Initialize();
                _pyState = PythonEngine.BeginAllowThreads();

                lifetime.ApplicationStopping.Register(() => {
                    try
                    {
                        PythonEngine.EndAllowThreads(_pyState);
                        PythonEngine.Shutdown();
                    }
                    catch { }
                });
            }
            else
            {
                logger.LogError($"Python dll not found at {pyLoc}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error when loading python dll {pyLoc}");
        }
    }
}
