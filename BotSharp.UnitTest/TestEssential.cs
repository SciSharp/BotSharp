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
        public static String BOT_ID = "2b6a288e-d891-40c6-96ce-6a0cf324545c";
        public static String BOT_CLIENT_TOKEN = "23a53c46d6244840bbb10c89c171d299";
        public static String BOT_DEVELOPER_TOKEN = "d86103f446d049ff8d5f506e8dfe5f3f";
        public static String BOT_NAME = "VirtualAssistant";

        protected Database dc { get; set; }
        protected string contentRoot;

        public TestEssential()
        {
            contentRoot = $"{Directory.GetCurrentDirectory()}\\..\\..\\..\\..\\Bot.WebStarter";

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
