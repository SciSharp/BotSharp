using BotSharp.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.NERs
{
    public class DucklingEntityRecognizer : INlpNer
    {
        public List<OntologyEnum> Ontologies => throw new NotImplementedException();

        public IConfiguration Configuration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PipeSettings Settings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
