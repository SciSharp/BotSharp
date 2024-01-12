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
    public string Id => "89746428-e2a1-415d-a9da-5eeaee8bb358";
    public string Name => "PaddlePaddle";
    public string Description => "An Open-Source Deep Learning Platform Originated from Industrial Practice";
    public string IconUrl => "https://miro.medium.com/v2/resize:fit:549/1*oZeecXkOoTzEYp-btIKwxw.jpeg";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new PaddleSharpSettings();
        config.Bind("PaddleSharp", settings);
        services.AddSingleton(x => settings);
        services.AddSingleton<IPdf2TextConverter, Pdf2TextConverter>();
    }
}
