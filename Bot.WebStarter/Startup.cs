using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Rasa.Console;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.WebStarter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            string db = RasaConsole.Configuration.GetSection("Database:Default").Value;
            RasaConsole.Options = new RasaOptions
            {
                HostUrl = Configuration.GetSection("Rasa:Host").Value,
                Assembles = new String[] { "Bot.Rasa" },
                ContentRootPath = env.ContentRootPath,
                DbName = db,
                DbConnectionString = Configuration.GetSection("Database:ConnectionStrings")[db]
            };
        }
    }
}
