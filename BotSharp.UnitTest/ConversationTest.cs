using BotSharp.Core.Engines;
using BotSharp.Core.Engines.Dialogflow;
using BotSharp.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.UnitTest
{
    [TestClass]
    public class ConversationTest : TestEssential
    {
        [TestMethod]
        public void TextRequest()
        {
            var ai = new ApiAi();
            var agent = ai.LoadAgent(BOT_ID);
            ai.AiConfig = new AIConfiguration(agent.ClientAccessToken, SupportedLanguage.English) { AgentId = BOT_ID };
            ai.AiConfig.SessionId = Guid.NewGuid().ToString();

            // Round 1
            var response = ai.TextRequest(new AIRequest { Query = new String[] { "Can you play country music?" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "music.play");
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "music-player-control").Lifespan, 3);
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "play-music").Lifespan, 5);
            Assert.AreEqual(response.Result.Parameters.First(x => x.Key == "genre").Value, "country");

            // Round 2
            response = ai.TextRequest(new AIRequest { Query = new String[] { "pause it" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "music_player_control.pause");
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "music-player-control").Lifespan, 3);
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "play-music").Lifespan, 4);

            // Round 3
            response = ai.TextRequest(new AIRequest { Query = new String[] { "continue" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "music_player_control.resume");
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "music-player-control").Lifespan, 3);
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "play-music").Lifespan, 3);

            // Round 4
            response = ai.TextRequest(new AIRequest { Query = new String[] { "play Hard Times by David Newman" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "music.play");
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "music-player-control").Lifespan, 3);
            Assert.AreEqual(response.Result.Contexts.First(x => x.Name == "play-music").Lifespan, 5);
            Assert.AreEqual(response.Result.Parameters.First(x => x.Key == "song").Value, "Hard Times");
            Assert.AreEqual(response.Result.Parameters.First(x => x.Key == "artist").Value, "David Newman");
        }
    }
}
