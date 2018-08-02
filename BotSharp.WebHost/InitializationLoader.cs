using BotSharp.Core.Abstractions;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.WebHost
{
    public class InitializationLoader
    {
        public IHostingEnvironment Env { get; set; }
        public IConfiguration Config { get; set; }
        public void Load()
        {
            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");
            var appsLoaders1 = TypeHelper.GetInstanceWithInterface<IInitializationLoader>(assemblies);
            appsLoaders1.ForEach(loader => {
                loader.Initialize(Config, Env);
            });
        }
    }
}
