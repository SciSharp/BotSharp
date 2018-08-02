using BotSharp.Core.Abstractions;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.Core.Accounts
{
    public class AccountDbInitializer : IHookDbInitializer
    {
        public int Priority => 100;

        public void Load(Database dc)
        {
            ImportAccount(dc);
        }

        private void ImportAccount(Database dc)
        {
            var dataPath = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Accounts");
        }
    }
}
