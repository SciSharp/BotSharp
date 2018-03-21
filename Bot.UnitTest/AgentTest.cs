using Bot.Rasa;
using Bot.Rasa.Agents;
using Bot.Rasa.Consoles;
using Bot.Rasa.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.UnitTest
{
    [TestClass]
    public class AgentTest : TestEssential
    {
        public static String BOT_ID = "2b6a288e-d891-40c6-96ce-6a0cf324545c";
        public static String BOT_NAME = "VirtualAssistant";

        [TestMethod]
        public void CreateAgent()
        {
            var rasa = new RasaAi(dc);
            var importer = new AgentImporterInDialogflow();
            var agent = rasa.RestoreAgent(importer, BOT_NAME);
            agent.Id = BOT_ID;

            int row = dc.DbTran(() => rasa.SaveAgent(agent));

            var loadedAgent = rasa.LoadAgent(agent.Id);
            Assert.IsTrue(loadedAgent.Intents.Count == agent.Intents.Count);
        }

        [TestMethod]
        public void TextRequest()
        {
            var rasa = new RasaAi(dc);
            rasa.agent = rasa.LoadAgent(BOT_ID);

            var response = rasa.TextRequest(dc, new AIRequest { Query = new String[] { "Create a work order for PetSmart" } });
            Assert.IsTrue(response.Result.Metadata.IntentName == "Create Work Order");

            response = rasa.TextRequest(dc, new AIRequest { Query = new String[] { "1010" } });
            Assert.IsTrue(response.Result.Metadata.IntentName == "Telling Store Number");
        }

        [TestMethod]
        public void Train()
        {
            var rasa = new RasaAi(dc);
        }
    }
}
