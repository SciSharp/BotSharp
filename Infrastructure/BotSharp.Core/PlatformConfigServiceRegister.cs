using BotSharp.Platform.Abstractions;
using Colorful;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Console = Colorful.Console;

namespace BotSharp.Core
{
    public class PlatformConfigServiceRegister
    {
        public static void Register<ISettings>(string section, IServiceCollection services, IConfiguration config) 
            where ISettings : IPlatformSettings, new()
        {
            var setting = new ISettings();
            config.GetSection(section).Bind(setting);
            services.AddSingleton<IPlatformSettings>(setting);

            Formatter[] settings = new Formatter[]
            {
                new Formatter(setting.BotEngine, Color.Yellow),
                new Formatter(setting.AgentStorage, Color.Yellow)
            };

            Console.WriteLineFormatted("NLU engine: {0}, Agent Storage: {1}.", Color.White, settings);
            Console.WriteLine();
        }
    }
}
