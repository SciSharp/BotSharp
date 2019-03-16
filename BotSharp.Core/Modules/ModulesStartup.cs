using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Console = Colorful.Console;

namespace BotSharp.Core.Modules
{
    /// <summary>
    /// Startup class for configurable modules 
    /// </summary>
    public class ModulesStartup
    {
        private readonly IEnumerable<IModule> _modules;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Create an instance of <see cref="ModulesStartup"/>
        /// </summary>
        /// <param name="configuration">
        /// Application configuration containing modules configuration
        /// </param>
        public ModulesStartup(IConfiguration configuration)
        {
            this._configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            ModulesOptions options = configuration.Get<ModulesOptions>();

            if (options.Modules.Count == 0)
            {
                options.Modules.Add(new ModuleOptions
                {
                    Name = "DialogflowAi",
                    Type = "BotSharp.Platform.Dialogflow"
                });

                Console.WriteLine($"Platform emulator not found.", Color.Red);
                Console.WriteLine($"Using default emulator: DialogflowAi, BotSharp.Platform.Dialogflow.");
            }

            this._modules = options.Modules
                .Select(s =>
                {
                    Type type = Type.GetType($"{s.Type}.ModuleInjector, {s.Type}");

                    if (type == null)
                    {
                        /* hard to debug in docker */
                        /* throw new TypeLoadException(
                            $"Cannot load type \"{s.Type}\"");*/
                        Console.WriteLine($"Cannot load type \"{s.Type}\"", Color.Red);
                        return null;
                    }
                    else
                    {
                        IModule module = (IModule)Activator.CreateInstance(type);
                        Console.WriteLine($"Loaded {s.Type.Split('.')[1]} \"{s.Type}\"", Color.Green);
                        return module;
                    }
                }
            );
        }

        /// <summary>
        /// Configurates the services.
        /// </summary>
        /// <param name="services">
        /// Instance of <see cref="IServiceCollection"/>.
        /// </param>
        /// <returns>
        /// Instance of <see cref="IServiceProvider"/>.
        /// </returns>
        public void ConfigureServices(IServiceCollection services)
        {
            foreach (IModule module in this._modules)
            {
                if (module == null) continue;
                module.ConfigureServices(services, this._configuration);
            }
        }

        /// <summary>
        /// Configures module services.
        /// </summary>
        /// <param name="app">
        /// Instance of <see cref="IApplicationBuilder"/>.
        /// </param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            foreach (IModule module in this._modules)
            {
                if (module == null) continue;
                module.Configure(app, env);
            }
        }
    }
}
