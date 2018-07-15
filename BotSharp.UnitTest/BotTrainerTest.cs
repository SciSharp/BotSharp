using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.UnitTest
{
    [TestClass]
    public class BotTrainerTest : TestEssential
    {
        [TestMethod]
        public void TrainingTest()
        {
            var config = new AIConfiguration("", SupportedLanguage.English) { AgentId = BOT_ID };
            config.SessionId = Guid.NewGuid().ToString();

            var rasa = new RasaAi();

            var trainer = new BotTrainer(BOT_ID, dc);
            trainer.Train(rasa.LoadAgent(BOT_ID));
        }
    }
}
