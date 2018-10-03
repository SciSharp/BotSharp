using Colorful;
using DotNetToolkit.JwtHelper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Console = Colorful.Console;

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
            /*services.AddTransient<IBotEngine>((provider) =>
            {
                return instance;
            });*/
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());

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

                Console.WriteLine($"{info.Title} {info.Version} {info.License.Name}", Color.Gray);
                Console.WriteLine($"{info.Description}", Color.Gray);
                Console.WriteLine($"{info.Contact.Name}", Color.Gray);
            });

            app.Use(async (context, next) =>
            {
                string token = context.Request.Headers["Authorization"];
                if (!string.IsNullOrWhiteSpace(token) && (token = token.Split(' ').Last()).Length == 32)
                {
                    var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
                    context.Request.Headers["Authorization"] = token;

                    context.Request.Headers["Authorization"] = "Bearer " + JwtToken.GenerateToken(config, token);
                }

                await next.Invoke();
            });
            app.UseAuthentication();

            app.UseMvc();

            AppDomain.CurrentDomain.SetData("DataPath", Path.Combine(env.ContentRootPath, "App_Data"));
            AppDomain.CurrentDomain.SetData("Configuration", Configuration);
            AppDomain.CurrentDomain.SetData("ContentRootPath", env.ContentRootPath);
            AppDomain.CurrentDomain.SetData("Assemblies", Configuration.GetValue<String>("Assemblies").Split(','));

            /*InitializationLoader loader = new InitializationLoader();
            loader.Env = env;
            loader.Config = Configuration;
            loader.Load();*/

            var platform = Configuration.GetValue<string>("Platform");
            var engine = Configuration.GetValue<string>($"{platform}:BotEngine");
            Formatter[] settings = new Formatter[]
            {
                new Formatter(platform, Color.Yellow),
                new Formatter(engine, Color.Yellow),
            };

            Console.WriteLineFormatted("Platform Emulator: {0} powered by {1} NLU engine.", Color.White, settings);
        }
    }
}