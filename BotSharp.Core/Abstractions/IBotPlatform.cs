using BotSharp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface IBotPlatform
    {
        AIResponse TextRequest(AIRequest request);
        void Train();
    }
}
