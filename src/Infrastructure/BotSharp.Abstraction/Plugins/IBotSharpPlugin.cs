using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Abstraction.Plugins;

public interface IBotSharpPlugin
{
    string Name => "";
    string Description => "";
    void RegisterDI(IServiceCollection services, IConfiguration config);
}
