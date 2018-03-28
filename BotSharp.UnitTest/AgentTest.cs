using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.UnitTest
{
    [TestClass]
    public class AgentTest : TestEssential
    {
        [TestMethod]
        public void CreateAgent()
        {
            var agent = new Agent
            {
                Id = BOT_ID,
                Name = BOT_NAME,
                Language = "en"
            };
            var rasa = new RasaAi(dc);
            
            int row = dc.DbTran(() => rasa.SaveAgent(agent));
        }

        [TestMethod]
        public void UpdateAgent()
        {
            var agent = new Agent
            {
                Id = BOT_ID,
                Name = BOT_NAME,
                Language = "en"
            };
            var rasa = new RasaAi(dc);

            int row = dc.DbTran(() => rasa.SaveAgent(agent));
        }

        [TestMethod]
        public void RestoreAgent()
        {
            var rasa = new RasaAi(dc);
            var importer = new AgentImporterInDialogflow();

            var agent = rasa.RestoreAgent(importer, BOT_NAME);
            agent.Id = BOT_ID;
            agent.ClientAccessToken = BOT_CLIENT_TOKEN;
            agent.DeveloperAccessToken = BOT_DEVELOPER_TOKEN;

            int row = dc.DbTran(() => rasa.SaveAgent(agent));
        }

        [TestMethod]
        public void Train()
        {
            var rasa = new RasaAi(dc);
        }
    }
}
