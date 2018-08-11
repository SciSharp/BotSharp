using BotSharp.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.NERs
{
    public class DucklingEntityRecognizer : INlpNer
    {
        public List<OntologyEnum> Ontologies => throw new NotImplementedException();
    }
}
