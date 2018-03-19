using Bot.Rasa;
using Bot.Rasa.Agents;
using Bot.Rasa.Consoles;
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
        public static String PIZZA_BOT_ID = "2b6a288e-d891-40c6-96ce-6a0cf324545c";
        
        [TestMethod]
        public void CreateAgent()
        {
            var rasa = new RasaConsole(dc);

            var agent = rasa.RestoreAgent(PIZZA_BOT_ID);

            int row = dc.DbTran(() => rasa.CreateAgent(agent));

            if(row > 0)
            {
                var result = rasa.Train(dc, agent.Id);
            }

            var loadedAgent = rasa.LoadAgent(agent.Id);
            Assert.IsTrue(loadedAgent.Intents.Count == agent.Intents.Count);

            var response = rasa.TextRequest(agent.Id, "weather in Chicago tomorrow");
            Assert.IsTrue(response.Intent.Name == "weather");
        }

        [TestMethod]
        public void TextRequest()
        {
            var rasa = new RasaConsole(dc);
            var response = rasa.TextRequest(PIZZA_BOT_ID, "how old are you");
            response = rasa.TextRequest(PIZZA_BOT_ID, "where are you from");
            response = rasa.TextRequest(PIZZA_BOT_ID, "would you like some cookie");
        }

        [TestMethod]
        public void Train()
        {
            var rasa = new RasaConsole(dc);
            rasa.Train(dc, PIZZA_BOT_ID);
        }
    }
}
