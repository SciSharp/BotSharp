using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.NRules.Services;

public interface IRuleLoader
{
    void LoadFromDirectory(global::NRules.RuleSharp.RuleRepository repo, string path);
}
