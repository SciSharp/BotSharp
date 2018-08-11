using BotSharp.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.NERs
{
    public class WitAiEntityRecognizer : INlpNer
    {
        public List<OntologyEnum> Ontologies
        {
            get
            {
                return new List<OntologyEnum>
                {
                    OntologyEnum.DateTime,
                    OntologyEnum.Location
                };
            }
        }
    }
}
