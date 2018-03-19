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

            RasaConsole.Options = new RasaOptions
            {
                HostUrl = "http://192.168.56.101:5000",
                ContentRootPath = contentRoot,
                Assembles = new String[] { "Bot.Rasa" }
            };

            dc = new Database();

            dc.BindDbContext<IDbRecord, DbContext4Sqlite>(new DatabaseBind
            {
                MasterConnection = new SqliteConnection($"Data Source={RasaConsole.Options.ContentRootPath}\\App_Data\\bot-rasa.db"),
                CreateDbIfNotExist = true
            });
        }
    }


}
