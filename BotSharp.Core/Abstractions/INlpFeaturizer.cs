using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public interface INlpFeaturizer
    {
        IConfiguration Configuration { get; set; }

        List<decimal> Featurize(string text);
    }
}
