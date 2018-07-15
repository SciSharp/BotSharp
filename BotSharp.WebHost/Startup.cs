using System;
using System.IO;
using System.Linq;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace BotSharp.WebHost
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.AddMvc(options =>
            {
                options.RespectBrowserAcceptHeader = true;
            }).AddJsonOptions(options =>
            {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            services.AddSwaggerGen(c =>
            {
                var info = Configuration.GetSection("Swagger").Get<Info>();
                c.SwaggerDoc(info.Version, info);

                var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "BotSharp.RestApi.xml");
                c.IncludeXmlComments(filePath);
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger(c =>
            {

            });
            app.UseSwaggerUI(c =>
            {
                var info = Configuration.GetSection("Swagger").Get<Info>();

                c.SupportedSubmitMethods(SubmitMethod.Get, SubmitMethod.Post, SubmitMethod.Put, SubmitMethod.Patch, SubmitMethod.Delete);
                c.ShowExtensions();
                c.SwaggerEndpoint(Configuration.GetValue<String>("Swagger:Endpoint"), info.Title);
                c.RoutePrefix = String.Empty;
                c.DocumentTitle = info.Title;
                c.InjectStylesheet(Configuration.GetValue<String>("Swagger:Stylesheet"));
            });

            app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());

            app.UseMvc();

            Database.Configuration = Configuration;
            Database.ContentRootPath = env.ContentRootPath;
            Database.Assemblies = Configuration.GetValue<String>("Assemblies").Split(',');
        }
    }
}
