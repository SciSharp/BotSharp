using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.EmailHandler.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailHandler.Hooks
{
    public class EmailHandlerUtilityHook : IAgentUtilityHook
    {
        public void AddUtilities(List<string> utilities)
        {
            utilities.Add(Utility.EmailHandler);
        }
    }
}
