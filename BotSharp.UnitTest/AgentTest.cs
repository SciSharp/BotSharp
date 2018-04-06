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
        public void CreateAgentTes()
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
        public void UpdateAgentTest()
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
        public void RestoreAgentTest()
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
        public void TrainAgentTest()
        {
            var config = new AIConfiguration(BOT_CLIENT_TOKEN, SupportedLanguage.English);
            config.SessionId = Guid.NewGuid().ToString();

            var rasa = new RasaAi(dc, config);
            rasa.agent = rasa.LoadAgent();
            rasa.Train(dc);
        }
    }
}
