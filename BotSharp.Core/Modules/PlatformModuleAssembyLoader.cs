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

            // load platform emulator dynamically
            Console.WriteLine();

            var platformDllPath = Path.Combine(options.ModuleBasePath, module.Path, $"{module.Type}.dll");
            if (File.Exists(platformDllPath))
            {
                Assembly library = AssemblyLoadContext.Default.LoadFromAssemblyPath(platformDllPath);
                action(library);

                Formatter[] settings = new Formatter[]
                {
                    new Formatter(platform, Color.Yellow),
                    new Formatter(platformDllPath, Color.Yellow)
                };
                Console.WriteLineFormatted("Loaded {0} platform emulator from {1} assembly.", Color.White, settings);
            }
            else
            {
                Console.WriteLine($"Can't load {module.Type} assembly from {platformDllPath}.");
            }

            Console.WriteLine();
        }
    }
}
