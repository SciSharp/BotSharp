using Bot.Rasa.Console;
using EntityFrameworkCore.BootKit;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace Bot.UnitTest
{
    public abstract class TestEssential
    {
        protected Database dc { get; set; }

        public TestEssential()
        {
            RasaConsole.Options = new RasaOptions
            {
                HostUrl = "http://192.168.56.101:5000",
                ContentRootPath = $"{Directory.GetCurrentDirectory()}\\..\\..\\..\\..\\Bot.WebStarter",
                Assembles = new String[] { "Bot.Rasa" }
            };

            dc = new Database();

            dc.BindDbContext<IDbRecord, DbContext4Sqlite>(new DatabaseBind
            {
                MasterConnection = new SqliteConnection($"Data Source={RasaConsole.Options.ContentRootPath}\\App_Data\\bot-rasa.db"),
                CreateDbIfNotExist = true,
                AssemblyNames = new string[] { "Bot.Rasa" }
            });
        }
    }


}
