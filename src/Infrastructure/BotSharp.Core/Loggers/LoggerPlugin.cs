using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Loggers;

public class LoggerPlugin : IBotSharpPlugin
{
    public string Id => "ea1aade7-7e29-4f13-a78b-2b1835aa4fea";
    public string Name => "Logger";
    public string Description => "Provide log service";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ILoggerService, LoggerService>();
    }
}