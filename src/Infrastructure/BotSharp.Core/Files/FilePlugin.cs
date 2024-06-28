using BotSharp.Core.Files.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Files;

public class FilePlugin : IBotSharpPlugin
{
    public string Id => "6a8473c0-04eb-4346-be32-24755ce5973d";

    public string Name => "File";

    public string Description => "Provides file analysis.";


    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IBotSharpFileService, BotSharpFileService>();

        services.AddScoped<IAgentHook, FileAnalyzerHook>();
        services.AddScoped<IAgentToolHook, FileAnalyzerToolHook>();
    }
}
