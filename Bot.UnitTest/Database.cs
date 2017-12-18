using CustomEntityFoundation;
using System;
using System.IO;

namespace Bot.UnitTest
{
    public abstract class Database
    {
        protected EntityDbContext dc { get; set; }

        public Database()
        {
            EntityDbContext.Assembles = new String[] { "Bot.Rasa" };
            var options = new DatabaseOptions
            {
                ContentRootPath = Directory.GetCurrentDirectory() + "\\..\\..\\..\\..",
            };

            // Sqlite
            options.Database = "Sqlite";
            options.ConnectionString = "Data Source=|DataDirectory|\\bot.db";
            EntityDbContext.Options = options;

            dc = new EntityDbContext();
            dc.InitDb();
        }
    }


}
