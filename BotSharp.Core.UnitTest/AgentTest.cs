using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.BotSharp;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.Core.UnitTest
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
                Language = "en"
            };
            var rasa = new RasaAi();
            //int row = dc.DbTran(() => rasa.agent.SaveAgent(dc));
        }

        [TestMethod]
        public void UpdateAgentTest()
        {
            var agent = new Agent
            {
                Id = BOT_ID,
                Language = "en"
            };
            var rasa = new RasaAi();
            //int row = dc.DbTran(() => rasa.agent.SaveAgent(dc));
        }

        [TestMethod]
        public void RestoreAgentFromDialogflowToRasaTest()
        {
            string dataPath = AppDomain.CurrentDomain.GetData("DataPath").ToString();
            var botsHeaderFilePath = Path.Combine(dataPath, "DbInitializer", $"Agents{Path.DirectorySeparatorChar}agents.json");
            var agents = JsonConvert.DeserializeObject<List<AgentImportHeader>>(File.ReadAllText(botsHeaderFilePath));

            agents.ForEach(agentHeader => {
                var bot = new BotSharpAi();
                bot.RestoreAgent<AgentImporterInDialogflow>(agentHeader);
            });
        }

        [TestMethod]
        public void TrainAgentTest()
        {
            var rasa = new RasaAi();
            rasa.LoadAgent(BOT_ID);
            rasa.Train();
        }
    }
}
