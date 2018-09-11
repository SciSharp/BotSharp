using BotSharp.Core.Abstractions;
using BotSharp.NLP.Tokenize;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var dataPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "DbInitializer", "Accounts");
            string json = File.ReadAllText(Path.Combine(dataPath, "users.json"));

            var users = JsonConvert.DeserializeObject<List<User>>(json);
            users.ForEach(user =>
            {
                if (!dc.Table<User>().Any(x => x.UserName == user.UserName))
                {
                    var core = new AccountCore(dc);
                    core.CreateUser(user);
                    core.Activate(user.Authenticaiton.ActivationCode);
                }
            });
            
        }
    }
}
