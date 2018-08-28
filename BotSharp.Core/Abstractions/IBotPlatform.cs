using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// Load agent from files.
        /// There must contain a meta.json
        /// </summary>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        Agent LoadAgentFromFile(string dataDir);

        AIResponse TextRequest(AIRequest request);

        Task Train(BotTrainOptions options);
    }
}
