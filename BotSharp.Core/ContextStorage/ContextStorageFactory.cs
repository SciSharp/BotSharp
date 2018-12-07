using BotSharp.Platform.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.ContextStorage
{
    public class ContextStorageFactory<T> : IContextStorageFactory<T>
    {
        private readonly Func<string, IContextStorage<T>> func;
        private readonly IPlatformSettings platformSetting;

        public ContextStorageFactory(IPlatformSettings setting, Func<string, IContextStorage<T>> serviceAccessor)
        {
            this.func = serviceAccessor;
            this.platformSetting = setting;
        }

        public IContextStorage<T> Get()
        {
            IContextStorage<T> storage = null;
            string storageName = this.platformSetting.ContextStorage;
            storage = func(storageName);
            return storage as IContextStorage<T>;
        }
    }
}
