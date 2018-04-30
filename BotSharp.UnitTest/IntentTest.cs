using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.UnitTest
{
    [TestClass]
    public class IntentTest : TestEssential
    {
        [TestMethod]
        public void TextRequest()
        {
            var config = new AIConfiguration(BOT_CLIENT_TOKEN, SupportedLanguage.English);
            config.SessionId = Guid.NewGuid().ToString();

            var rasa = new RasaAi(dc, config);
        }
    }
}
