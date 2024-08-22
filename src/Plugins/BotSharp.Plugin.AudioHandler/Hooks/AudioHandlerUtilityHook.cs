using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.AudioHandler.Hooks;

public class AudioHandlerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.AudioHandler);
    }
}