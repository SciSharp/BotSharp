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

        protected Database dc { get; set; }
        protected string contentRoot;

        public TestEssential()
        {
            contentRoot = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}BotSharp.WebHost{Path.DirectorySeparatorChar}";

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var settings = Directory.GetFiles(contentRoot + $"Settings{Path.DirectorySeparatorChar}", "*.json");
            settings.ToList().ForEach(setting =>
            {
                configurationBuilder.AddJsonFile(setting, optional: false, reloadOnChange: true);
            });

            AppDomain.CurrentDomain.SetData("DataPath", Path.Join(contentRoot, "App_Data"));
            AppDomain.CurrentDomain.SetData("Configuration", configurationBuilder.Build());
            AppDomain.CurrentDomain.SetData("ContentRootPath", contentRoot);
            AppDomain.CurrentDomain.SetData("Assemblies", new String[] { "BotSharp.Core" });

            dc = new DefaultDataContextLoader().GetDefaultDc();
        }
    }


}
