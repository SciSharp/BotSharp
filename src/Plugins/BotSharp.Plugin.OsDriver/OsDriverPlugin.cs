using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.OsDriver;

public class OsDriverPlugin : IBotSharpPlugin
{
    public string Id => "5aef4940-2f95-464b-ad37-43dbf89febf0";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        
    }
}
