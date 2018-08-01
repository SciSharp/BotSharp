using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface IBotPlatform
    {
        /// <summary>
        /// Load agent profile
        /// </summary>
        /// <param name="id">agentId, clientAccessToken, developerAccessToken</param>
        /// <returns></returns>
        Agent LoadAgent(string id);

        AIResponse TextRequest(AIRequest request);

        void Train();
    }
}
