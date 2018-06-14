using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface INlpProvider
    {
        IConfiguration Configuration { get; set; }

        bool Load();
    }
}
