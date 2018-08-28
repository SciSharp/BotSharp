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
                    var settings = Directory.GetFiles(Path.Combine(env.ContentRootPath, "Settings"), "*.json");
                    settings.ToList().ForEach(setting =>
                    {
                        config.AddJsonFile(setting, optional: false, reloadOnChange: true);
                    });
                })
#if RASA_UI
                .UseUrls("http://0.0.0.0:5000")
#else
                .UseUrls("http://0.0.0.0:3112")
#endif
                .UseStartup<Startup>()
                .Build();
    }
}
