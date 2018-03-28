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

            var response = rasa.TextRequest(new AIRequest { Query = new String[] { "Create a work order for PetSmart" } });
            Assert.IsTrue(response.Result.Metadata.IntentName == "Create Work Order");

            response = rasa.TextRequest(new AIRequest { Query = new String[] { "1010" } });
            Assert.IsTrue(response.Result.Metadata.IntentName == "Telling Store Number");
        }
    }
}
