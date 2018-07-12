using BotSharp.Core.Engines;
using EntityFrameworkCore.BootKit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace BotSharp.UnitTest
{
    public abstract class TestEssential
    {
        public static String BOT_ID = "6a9fd374-c43d-447a-97f2-f37540d0c725";
        public static String BOT_CLIENT_TOKEN = "43a0f48e3f1e41da822092e7e699426b";
        public static String BOT_DEVELOPER_TOKEN = "cd1e4685c6a04d7db1f59e6853fd597b";
        public static String BOT_NAME = "Spotify";

        protected Database dc { get; set; }
        protected string contentRoot;

        public TestEssential()
        {
            contentRoot = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..";

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var settings = Directory.GetFiles(contentRoot + $"{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}", "*.json");
            settings.ToList().ForEach(setting =>
            {
                configurationBuilder.AddJsonFile(setting, optional: false, reloadOnChange: true);
            });
            Database.Configuration = configurationBuilder.Build();

            Database.Assemblies = new String[] { "BotSharp.Core" };
            Database.ContentRootPath = contentRoot;

            dc = new DefaultDataContextLoader().GetDefaultDc();
        }
    }


}
