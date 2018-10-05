using Colorful;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using Console = Colorful.Console;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PlatformServiceCollectionExtensions
    {
        public static IServiceCollection AddPlatformEmulator(this IServiceCollection services, IConfiguration configuration, Action<Assembly> action)
        {
            var platform = configuration.GetValue<string>("Platform");
            var platformAssemblyName = configuration.GetValue<string>("platformAssemblyName");
            var engine = configuration.GetValue<string>($"{platform}:BotEngine");

            Formatter[] settings = new Formatter[]
            {
                new Formatter(platform, Color.Yellow),
                new Formatter(platformAssemblyName, Color.Yellow),
                new Formatter(engine, Color.Yellow),
            };

            // load platform emulator dynamically
            Console.WriteLine();

            var platformDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{platformAssemblyName}.dll");
            if (File.Exists(platformDllPath))
            {
                Assembly library = Assembly.LoadFile(platformDllPath);
                action(library);
                Console.WriteLineFormatted("Loaded {0} platform emulator from {1} assembly which is using {2} engine.", Color.White, settings);
            }
            else
            {
                Console.WriteLine($"Can't load {platformAssemblyName} assembly.");
            }

            Console.WriteLine();

            /*Type myClass = (from type in library.GetExportedTypes()
                where typeof(IMyInterface).IsAssignableFrom(type)
                select type)
                .Single();*/

            return services;
        }
    }
}
