using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.WebDriver;

public class WebDriverPlugin : IBotSharpPlugin
{
    public string Name => "Web Driver";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        
    }
}
