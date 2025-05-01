using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Routing
{
    public interface IFunctionExecutor
    {
        public Task<bool> Execute(RoleDialogModel message);

        public Task<string> GetIndication(RoleDialogModel message);
    }
}
