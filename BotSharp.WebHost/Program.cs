using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace BotSharp.WebHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            Microsoft.AspNetCore.WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {

                    var env = hostingContext.HostingEnvironment;
                    var settings = Directory.GetFiles($"{env.ContentRootPath}{Path.DirectorySeparatorChar}Settings", "*.json");
                    settings.ToList().ForEach(setting =>
                    {
                        config.AddJsonFile(setting, optional: false, reloadOnChange: true);
                    });
                })
#if MODE_RASA
			    .UseUrls("http://0.0.0.0:5000")
#else
                .UseUrls("http://0.0.0.0:3112")
#endif
                .UseStartup<Startup>()
                .Build();
    }
}
