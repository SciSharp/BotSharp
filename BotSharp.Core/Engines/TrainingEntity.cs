using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class TrainingEntity
    {
        public virtual String EntityType { get; set; }

        public virtual String EntityValue { get; set; }

        public List<String> Synonyms { get; set; }
    }
}
