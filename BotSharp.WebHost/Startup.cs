using System;
using System.IO;
using System.Linq;
using BotSharp.Core.Engines;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using BotSharp.Core.Engines.BotSharp;
using System.Collections.Generic;
using Newtonsoft.Json;

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

            app.Use(async (context, next) =>
            {
                string token = context.Request.Headers["Authorization"];
                if (string.IsNullOrWhiteSpace(token))
                {
                }

                await next.Invoke();
            });
            app.UseAuthentication();

            app.UseMvc();

            AppDomain.CurrentDomain.SetData("DataPath", Path.Join(env.ContentRootPath, "App_Data"));
            AppDomain.CurrentDomain.SetData("Configuration", Configuration);
            AppDomain.CurrentDomain.SetData("ContentRootPath", env.ContentRootPath);
            AppDomain.CurrentDomain.SetData("Assemblies", Configuration.GetValue<String>("Assemblies").Split(','));

            InitializationLoader loader = new InitializationLoader();
            loader.Env = env;
            loader.Config = Configuration;
            loader.Load();

            /*Runcmd();
            var ai = new BotSharpAi();
            ai.LoadAgent("6a9fd374-c43d-447a-97f2-f37540d0c725");
            ai.Train();*/
        }

        public void Runcmd () 
        {
            string cmd = "/home/bolo/Desktop/BotSharp/TrainingFiles/crfsuite learn -m /home/bolo/Desktop/BotSharp/TrainingFiles/bolo.model /home/bolo/Desktop/BotSharp/TrainingFiles/1.txt";
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "sh";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = false;//不显示程序窗口
            p.Start();//启动程序

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(cmd + "&exit");

            p.StandardInput.AutoFlush = false;
            //p.StandardInput.WriteLine("exit");
            //向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
            //同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令



            //获取cmd窗口的输出信息
            string output = p.StandardOutput.ReadToEnd();

            //StreamReader reader = p.StandardOutput;
            //string line=reader.ReadLine();
            //while (!reader.EndOfStream)
            //{
            //    str += line + "  ";
            //    line = reader.ReadLine();
            //}

            p.WaitForExit();//等待程序执行完退出进程
            p.Close();


            Console.WriteLine(output);
        }
    }
}