using BotSharp.Core;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.AgentStorage
{
    public class AgentStorageFactory<TAgent> : IAgentStorageFactory<TAgent> where TAgent : AgentBase
    {
        private readonly Func<string, IAgentStorage<TAgent>> func;
        private readonly IPlatformSettings platformSetting;

        public AgentStorageFactory(IPlatformSettings setting, Func<string, IAgentStorage<TAgent>> serviceAccessor)
        {
            this.func = serviceAccessor;
            this.platformSetting = setting;
        }

        public IAgentStorage<TAgent> Get()
        {
            IAgentStorage<TAgent> storage = null;
            string storageName = this.platformSetting.AgentStorage;
            storage = func(storageName);
            return storage as IAgentStorage<TAgent>;
        }      
    }
}
