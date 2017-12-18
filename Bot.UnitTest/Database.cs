using CustomEntityFoundation;
using CustomEntityFoundation.Entities;
using EntityFrameworkCore.BootKit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
