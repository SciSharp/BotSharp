using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.EmailReader.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailReader.Hooks;

public class EmailReaderUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.EmailReader);
    }
}
