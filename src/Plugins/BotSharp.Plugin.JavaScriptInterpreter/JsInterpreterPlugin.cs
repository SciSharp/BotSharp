using BotSharp.Abstraction.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.JavaScriptInterpreter;

public class JsInterpreterPlugin : IBotSharpAppPlugin
{
    public string Id => "7a5a8cd7-26d9-4ac3-9d79-d02084bea372";
    public string Name => "JavaScript Interpreter";
    public string Description => "";
    public string? IconUrl => "";

    public void Configure(IApplicationBuilder app)
    {
        
    }

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        
    }
}
