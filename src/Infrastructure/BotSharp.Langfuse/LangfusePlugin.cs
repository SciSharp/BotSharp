using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Langfuse
{
    public class LangfusePlugin : IBotSharpPlugin
    {
        public string Id => throw new NotImplementedException();

        public void RegisterDI(IServiceCollection services,IConfiguration config)
        {
            throw new NotImplementedException();
        }
    }
}
