using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Crontab.Abstraction
{
    public interface ICrontabAuthenticationHook
    {
        void SetUserIdentity(CrontabItem item);
    }
}
