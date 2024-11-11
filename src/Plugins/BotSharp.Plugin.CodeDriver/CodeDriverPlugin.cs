using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.CodeDriver;

public class CodeDriverPlugin : IBotSharpPlugin
{
    public string Id => "c0dedea7-70e3-4c35-a047-18479d7c403e";
    public string Name => "Code Driver";
    public string Description => "Convert the user requirements into corresponding SQL statements";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/3176/3176315.png";

    public string[] AgentIds = 
    [ 
        BuiltInAgentId.CodeDriver
    ];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        
    }
}
