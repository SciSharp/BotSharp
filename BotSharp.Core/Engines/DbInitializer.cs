using BotSharp.Core.Abstractions;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class DbInitializer : IInitializationLoader
    {
        public int Priority => 1;

        public void Initialize(IConfiguration config, IHostingEnvironment env)
        {
            var dc = new DefaultDataContextLoader().GetDefaultDc();

            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");
            var instances = TypeHelper.GetInstanceWithInterface<IHookDbInitializer>(assemblies);

            // initial app db order by priority
            instances.OrderBy(x => x.Priority).ToList()
                .ForEach(instance =>
                {
                    Console.WriteLine($"DbInitializer: {instance.ToString()}");
                    dc.Transaction<IDbRecord>(() => instance.Load(dc));
                });
        }
    }
}
