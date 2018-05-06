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

            // Round 1
            var response = rasa.TextRequest(new AIRequest { Query = new String[] { "Hi, Voiceweb" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "Wakeup");

            // Round 2
            response = rasa.TextRequest(new AIRequest { Query = new String[] { "I'm going to apple store to buy iphone 10" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "Transfer2SalesBot");

            // Round 3
            response = rasa.TextRequest(new AIRequest { Query = new String[] { "Yes" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "Transfer2SalesBot - address");

            // Round 4
            response = rasa.TextRequest(new AIRequest { Query = new String[] { "Sure" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "Transfer2SalesBot - confirm address");

            // Round 5
            response = rasa.TextRequest(new AIRequest { Query = new String[] { "That's right" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "Transfer2SalesBot - payment");

            // Round 6
            response = rasa.TextRequest(new AIRequest { Query = new String[] { "Yes" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "Transfer2SalesBot - place work order");

            // Round 7
            response = rasa.TextRequest(new AIRequest { Query = new String[] { "byebye" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "Byebye");
        }
    }
}
