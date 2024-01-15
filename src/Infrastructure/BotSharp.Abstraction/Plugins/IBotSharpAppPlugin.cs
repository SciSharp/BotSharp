using Microsoft.AspNetCore.Builder;

namespace BotSharp.Abstraction.Plugins
{
    public interface IBotSharpAppPlugin: IBotSharpPlugin
    {
        void Configure(IApplicationBuilder app);
    }
}
