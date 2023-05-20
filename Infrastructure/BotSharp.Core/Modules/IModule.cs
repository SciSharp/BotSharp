using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Core.Modules
{
    /// <summary>
    /// Represents a configurable module containing multiple servies.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Configurates the module services.
        /// </summary>
        /// <param name="services">
        /// Instance of <see cref="IServiceCollection"/>.
        /// </param>
        /// <returns>
        /// Instance of <see cref="IServiceProvider"/>.
        /// </returns>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Configures module services.
        /// </summary>
        /// <param name="app">
        /// Instance of <see cref="IApplicationBuilder"/>.
        /// </param>
        void Configure(IApplicationBuilder app, IWebHostEnvironment env);
    }
}
