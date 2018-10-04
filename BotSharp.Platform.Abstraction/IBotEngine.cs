using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Abstraction
{
    /// <summary>
    /// BotEngine is the NLU of bot platform
    /// </summary>
    public interface IBotEngine
    {
        AiResponse TextRequest(AiRequest request);

        Task Train(BotTrainOptions options);
    }
}
