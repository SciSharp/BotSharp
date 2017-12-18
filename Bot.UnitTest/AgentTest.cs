using Bot.Rasa;
using Bot.Rasa.Agents;
using Bot.Rasa.Console;
using EntityFrameworkCore.BootKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.UnitTest
{
    [TestClass]
    public class AgentTest : Database
    {
        public static String PIZZA_BOT_ID = "2b6a288e-d891-40c6-96ce-6a0cf324545c";
        public static RasaOptions Options = new RasaOptions { HostUrl = "http://192.168.56.101:5000" };

        [TestMethod]
        public void CreateAgent()
        {
            var rasa = new RasaConsole(dc, Options);

            var agent = new RasaAgent
            {
                Id = PIZZA_BOT_ID,
                Name = "Pizza Bot"
            };

            dc.DbTran(() => rasa.CreateAgent(agent));
        }

        [TestMethod]
        public void TextRequest()
        {
            var rasa = new RasaConsole(dc, Options);
            var response = rasa.TextRequest(PIZZA_BOT_ID, "how old are you");
            response = rasa.TextRequest(PIZZA_BOT_ID, "where do you come from");
            response = rasa.TextRequest(PIZZA_BOT_ID, "would you like some cookie");
        }

        [TestMethod]
        public void Train()
        {
            var rasa = new RasaConsole(dc, Options);
            rasa.Train(PIZZA_BOT_ID);
        }
    }
}
