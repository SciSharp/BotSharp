using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using DotNetToolkit;
using DotNetToolkit.JwtHelper;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            services.AddJwtAuth(Configuration);

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
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    In = "header",
                    Description = "Please insert JWT with Bearer schema. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    Type = "apiKey"
                });

                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                  { "Bearer", Enumerable.Empty<string>() },
                });

                var info = Configuration.GetSection("Swagger").Get<Info>();
                c.SwaggerDoc(info.Version, info);

                var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "BotSharp.RestApi.xml");
                c.IncludeXmlComments(filePath);
                c.OperationFilter<SwaggerFileUploadOperation>();
            });

            // register platform dependency
            services.AddTransient<IBotPlatform>((provider) =>
            {
                var assemblies = (String[])AppDomain.CurrentDomain.GetData("Assemblies");
                var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
                var implements = TypeHelper.GetClassesWithInterface<IBotPlatform>(assemblies);
                string platform = config.GetValue<String>("BotPlatform");
                var implement = implements.FirstOrDefault(x => x.Name.Split('.').Last() == platform);
                var instance = (IBotPlatform)Activator.CreateInstance(implement);

                return instance;
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

            app.UseSwagger();
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

            app.Use(async (context, next) =>
            {
                string token = context.Request.Headers["Authorization"];
                if (!string.IsNullOrWhiteSpace(token) && (token = token.Split(' ').Last()).Length == 32)
                {
                    var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
                    context.Request.Headers["ClientAccessToken"] = token;

                    // context.Request.Headers["Authorization"] = "Bearer " + JwtToken.GenerateToken(config, userId);
                }

                await next.Invoke();
            });
            app.UseAuthentication();

            app.UseMvc();

            AppDomain.CurrentDomain.SetData("DataPath", Path.Combine(env.ContentRootPath, "App_Data"));
            AppDomain.CurrentDomain.SetData("Configuration", Configuration);
            AppDomain.CurrentDomain.SetData("ContentRootPath", env.ContentRootPath);
            AppDomain.CurrentDomain.SetData("Assemblies", Configuration.GetValue<String>("Assemblies").Split(','));

            InitializationLoader loader = new InitializationLoader();
            loader.Env = env;
            loader.Config = Configuration;
            loader.Load();
        }
    }
}