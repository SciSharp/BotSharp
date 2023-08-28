using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.PaddleSharp.Providers;
using BotSharp.Plugin.PaddleSharp.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Plugin.PaddleOCR;

public class PaddleSharpPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new PaddleSharpSettings();
        config.Bind("PaddleSharp", settings);
        services.AddSingleton(x => settings);
        services.AddSingleton<IPdf2TextConverter, Pdf2TextConverter>();
    }
}
