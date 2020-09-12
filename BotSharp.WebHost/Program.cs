using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;

namespace BotSharp.WebHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webHost =>
                {
                    webHost
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        var env = hostingContext.HostingEnvironment;
                        string dir = Path.GetFullPath(env.ContentRootPath);
                        string settingsFolder = Path.Combine(dir, "Settings");

                        // locate setting folder
                        if (!Directory.Exists(settingsFolder))
                        {
                            dir = Path.GetFullPath(env.ContentRootPath + "/..");
                        }

                        settingsFolder = Path.Combine(dir, "Settings");

                        if (!Directory.Exists(settingsFolder))
                        {
                            dir = Path.GetFullPath(env.ContentRootPath + "/bin");
                        }

                        settingsFolder = Path.Combine(dir, "Settings");

                        Console.WriteLine($"Read settings from {settingsFolder}");

                        var settings = Directory.GetFiles(settingsFolder, "*.json");
                        settings.ToList().ForEach(setting =>
                        {
                            config.AddJsonFile(setting, optional: false, reloadOnChange: true);
                        });
                    })
                    .UseUrls("http://0.0.0.0:3112")
                    .UseStartup<Startup>();

                });
        }
    }
}
