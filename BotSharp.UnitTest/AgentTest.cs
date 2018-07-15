using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
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
                Language = "en",
                UserId = Guid.NewGuid().ToString()
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
            var botsHeaderFilePath = $"{Database.ContentRootPath}App_Data{Path.DirectorySeparatorChar}DbInitializer{Path.DirectorySeparatorChar}Agents{Path.DirectorySeparatorChar}agents.json";
            var agents = JsonConvert.DeserializeObject<List<AgentImportHeader>>(File.ReadAllText(botsHeaderFilePath));

            agents.ForEach(agentHeader => {
                var rasa = new RasaAi();
                rasa.RestoreAgent<AgentImporterInDialogflow>(agentHeader);
            });
        }

        [TestMethod]
        public void TrainAgentTest()
        {
            var config = new AIConfiguration("", SupportedLanguage.English) { AgentId = BOT_ID };
            config.SessionId = Guid.NewGuid().ToString();

            var rasa = new RasaAi();

            rasa.Train();
        }
    }
}
