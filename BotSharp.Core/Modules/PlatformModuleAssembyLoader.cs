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

            // load platform emulator dynamically
            Console.WriteLine();

            options.Modules.ForEach(module => {

                var dllPath = Path.Combine(options.ModuleBasePath, module.Path, $"{module.Type}.dll");
                if (File.Exists(dllPath))
                {
                    Assembly library = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
                    action(library);

                    Formatter[] settings = new Formatter[]
                    {
                        new Formatter(module.Name, Color.Yellow),
                        new Formatter(module.Type, Color.Yellow),
                        new Formatter(dllPath, Color.Yellow)
                    };
                    Console.WriteLineFormatted("Loaded {0} module, type: {1}, path: {2}", Color.White, settings);
                }
                else
                {
                    Console.WriteLine($"Can't load {module.Type} assembly from {dllPath}.");
                }

            });

            Console.WriteLine();
        }
    }
}
