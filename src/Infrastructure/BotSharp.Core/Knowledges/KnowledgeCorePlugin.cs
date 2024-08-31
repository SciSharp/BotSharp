using BotSharp.Core.Knowledges.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Knowledges;

public class KnowledgeCorePlugin : IBotSharpPlugin
{
    public string Id => "a5ebc8f9-d089-44d0-bf00-4eac97a050bc";

    public string Name => "Knowledge Core";

    public string Description => "Provides core knowledge services.";


    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IKnowledgeService, KnowledgeService>();
    }
}