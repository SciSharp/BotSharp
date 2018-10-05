using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            this._modules = options.Modules
                .Select(s =>
                {
                    Type type = Type.GetType(s.Type);

                    if (type == null)
                    {
                        throw new TypeLoadException(
                            $"Cannot load type \"{s.Type}\"");
                    }

                    IModule module = (IModule)Activator.CreateInstance(type);
                    return module;
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
                module.ConfigureServices(services, this._configuration);
            }
        }

        /// <summary>
        /// Configures module services.
        /// </summary>
        /// <param name="app">
        /// Instance of <see cref="IApplicationBuilder"/>.
        /// </param>
        public void Configure(IApplicationBuilder app)
        {
            foreach (IModule module in this._modules)
            {
                module.Configure(app);
            }
        }
    }
}
