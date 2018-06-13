using BotSharp.Core.Engines;
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
            var trainer = new BotTrainer(BOT_ID, dc);
            trainer.Train();
        }
    }
}
