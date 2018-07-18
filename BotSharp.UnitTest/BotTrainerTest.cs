using BotSharp.Core.Engines;
using BotSharp.Core.Engines.BotSharp;
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
            var ai = new BotSharpAi();
            ai.LoadAgent(BOT_ID);
            ai.Train();
        }
    }
}
