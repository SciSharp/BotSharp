using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Rules;

public interface IRuleLoader
{
    void LoadFromDirectory(global::NRules.RuleSharp.RuleRepository repo, string path);
}
