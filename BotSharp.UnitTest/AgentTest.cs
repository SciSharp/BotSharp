using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
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
        public void CreateAgentTest()
        {
            var agent = new Agent
            {
                Id = BOT_ID,
                Name = BOT_NAME,
                Language = "en",
                UserId = Guid.NewGuid().ToString()
            };
            var rasa = new RasaAi(dc);
            rasa.agent = agent;
            int row = dc.DbTran(() => rasa.agent.SaveAgent(dc));
        }

        [TestMethod]
        public void UpdateAgentTest()
        {
            var agent = new Agent
            {
                Id = BOT_ID,
                Name = BOT_NAME,
                Language = "en"
            };
            var rasa = new RasaAi(dc);
            rasa.agent = agent;
            int row = dc.DbTran(() => rasa.agent.SaveAgent(dc));
        }

        [TestMethod]
        public void RestoreAgentTest()
        {
            var rasa = new RasaAi(dc);
            var importer = new AgentImporterInDialogflow();

            string dataDir =  $"{Database.ContentRootPath}\\App_Data\\DbInitializer\\Agents\\";
            var agent = rasa.RestoreAgent(importer, BOT_NAME, dataDir);
            agent.Id = BOT_ID;
            agent.ClientAccessToken = BOT_CLIENT_TOKEN;
            agent.DeveloperAccessToken = BOT_DEVELOPER_TOKEN;
            agent.UserId = Guid.NewGuid().ToString();
            rasa.agent = agent;

            int row = dc.DbTran(() => rasa.agent.SaveAgent(dc));
        }

        [TestMethod]
        public void TrainAgentTest()
        {
            var config = new AIConfiguration(BOT_CLIENT_TOKEN, SupportedLanguage.English);
            config.SessionId = Guid.NewGuid().ToString();

            var rasa = new RasaAi(dc, config);

            string msg = rasa.Train();

            Assert.IsTrue(!String.IsNullOrEmpty(msg));
        }
    }
}
