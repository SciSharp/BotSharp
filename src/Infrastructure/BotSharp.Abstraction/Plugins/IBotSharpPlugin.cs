using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Abstraction.Plugins;

public interface IBotSharpPlugin
{
    void RegisterDI(IServiceCollection services, IConfiguration config);
}
