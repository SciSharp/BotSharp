using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Abstraction.Plugins
{
    public interface IBotSharpAppPlugin: IBotSharpPlugin
    {
        void Configure(IApplicationBuilder app);
    }
}
