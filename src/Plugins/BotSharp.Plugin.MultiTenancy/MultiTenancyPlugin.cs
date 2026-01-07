using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.MultiTenancy.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.MultiTenancy;

public class MultiTenancyPlugin: IBotSharpPlugin
{
    public string Id => "55adcb55-3d05-400e-92f2-65cdeefba360";
    public string Name => "MultiTenancy";
    public string Description => "Multi-tenancy support plugin";
    public string IconUrl => null;

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddMultiTenancy(config);
    }
}