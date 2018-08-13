using BotSharp.Core.Engines.NERs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Abstractions
{
    public interface INlpNer : INlpPipeline
    {
        List<OntologyEnum> Ontologies { get; }
    }
}
