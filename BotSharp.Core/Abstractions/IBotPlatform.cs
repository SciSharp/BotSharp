using BotSharp.Core.Agents;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines
{
    public interface IBotPlatform
    {
        AiResponse TextRequest(AiRequest request);

        Task Train(BotTrainOptions options);
    }
}
