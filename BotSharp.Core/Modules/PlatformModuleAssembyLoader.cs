using BotSharp.Core.Modules;
using Colorful;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Console = Colorful.Console;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PlatformModuleAssembyLoader
    {
        public static void LoadAssemblies(IConfiguration configuration, Action<Assembly> action)
        {
            ModulesOptions options = configuration.Get<ModulesOptions>();

            var platform = configuration.GetValue<string>("platformModuleName");
            var module = options.Modules.Find(x => x.Name == platform);
            var engine = configuration.GetValue<string>($"{platform}:BotEngine");

            Formatter[] settings = new Formatter[]
            {
                new Formatter(platform, Color.Yellow),
                new Formatter(module.Name, Color.Yellow),
                new Formatter(engine, Color.Yellow),
            };

            // load platform emulator dynamically
            Console.WriteLine();

            var platformDllPath = Path.Combine(options.ModuleBasePath, module.Path, $"{module.Type}.dll");
            if (File.Exists(platformDllPath))
            {
                Assembly library = AssemblyLoadContext.Default.LoadFromAssemblyPath(platformDllPath);
                action(library);
                Console.WriteLineFormatted("Loaded {0} platform emulator from {1} assembly which is using {2} engine.", Color.White, settings);
            }
            else
            {
                Console.WriteLine($"Can't load {module.Type} assembly.");
            }
            Console.WriteLine();
        }
    }
}
