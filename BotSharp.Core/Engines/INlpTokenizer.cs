using BotSharp.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    /// <summary>
    /// Segment text into words, punctuations marks etc.
    /// </summary>
    public interface INlpTokenizer
    {
        IConfiguration Configuration { get; set; }

        List<NlpToken> Tokenize(string text);
    }
}
