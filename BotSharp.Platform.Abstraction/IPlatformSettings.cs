using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Abstraction
{
    public interface IPlatformSettings
    {
        string BotEngine { get; set; }

        string AgentStorage { get; set; }

        string ContextStorage { get; set; }
    }
}
