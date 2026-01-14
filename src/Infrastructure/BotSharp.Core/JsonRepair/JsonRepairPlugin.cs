using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core.JsonRepair;

public class JsonRepairPlugin : IBotSharpPlugin
{
    public string Id => "b2e8f9c4-6d5a-4f28-cbe1-cf8b92e344cb";
    public string Name => "JSON Repair";
    public string Description => "Repair malformed JSON using LLM";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IJsonRepairService, JsonRepairService>();
    }
}

