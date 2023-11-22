using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Plugin.TelegramBots
{
    public class TelegramBotsPlugin : IBotSharpPlugin
    {
        public string Name => "Telegram Bots";
        public string Description => "Bots are small applications that run entirely within the Telegram app.";

        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            
        }
    }
}
