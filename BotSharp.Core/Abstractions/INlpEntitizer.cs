using BotSharp.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface INlpEntitizer
    {
        IConfiguration Configuration { get; set; }

        List<NlpEntity> Entitize(string text);
    }
}
