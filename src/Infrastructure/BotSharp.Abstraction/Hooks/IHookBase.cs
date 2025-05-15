using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Hooks
{
    public interface IHookBase
    {
        /// <summary>
        /// Agent Id
        /// </summary>
        string SelfId => string.Empty;
        bool IsMatch(string id) => string.IsNullOrEmpty(SelfId) || SelfId == id;
    }
}
