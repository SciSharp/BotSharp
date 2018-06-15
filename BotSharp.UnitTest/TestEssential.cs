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
        public static String BOT_ID = "5f98a0fd-e7e9-4155-9610-d3f40d026162";
        public static String BOT_CLIENT_TOKEN = "2fffb9a1a9214144ab2717a37fa43c33";
        public static String BOT_DEVELOPER_TOKEN = "2c7d224cf7274f9d93b4c65c31ca82fe";
        public static String BOT_NAME = "Handybot";

        protected Database dc { get; set; }
        protected string contentRoot;

        public TestEssential()
        {
            contentRoot = $"{Directory.GetCurrentDirectory()}\\..\\..\\..\\";

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var settings = Directory.GetFiles(contentRoot + "/Settings/", "*.json");
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
