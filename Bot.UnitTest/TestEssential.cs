using Bot.Rasa.Consoles;
using EntityFrameworkCore.BootKit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace Bot.UnitTest
{
    public abstract class TestEssential
    {
        protected Database dc { get; set; }
        protected string contentRoot;

        public TestEssential()
        {
            contentRoot = $"{Directory.GetCurrentDirectory()}\\..\\..\\..\\..\\Bot.WebStarter";

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var settings = Directory.GetFiles(contentRoot, "settings.*.json");
            settings.ToList().ForEach(setting =>
            {
                configurationBuilder.AddJsonFile(setting, optional: false, reloadOnChange: true);
            });
            Database.Configuration = configurationBuilder.Build();

            Database.Assemblies = new String[] { "Bot.Rasa" };
            Database.ContentRootPath = contentRoot;

            dc = new DefaultDataContextLoader().GetDefaultDc();

            RasaAi.Options = new RasaOptions
            {
                HostUrl = Database.Configuration.GetSection("Rasa:Host").Value,
                ContentRootPath = contentRoot,
                Assembles = new String[] { "Bot.Rasa" }
            };
        }
    }


}
