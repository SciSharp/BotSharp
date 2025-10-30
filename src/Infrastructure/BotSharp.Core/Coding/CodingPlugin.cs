using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Coding;

public class CodingPlugin : IBotSharpPlugin
{
    public string Id => "31bc334b-9462-4191-beac-cb4a139b78c1";
    public string Name => "Coding";
    public string Description => "Handling execution and generation of code scripts";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<CodeScriptExecutor>();
    }
}
