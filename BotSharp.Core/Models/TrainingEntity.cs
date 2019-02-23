using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models
{
    public class TrainingEntity
    {
        public virtual String Entity { get; set; }

        public List<TrainingEntitySynonym> Values { get; set; }
    }
}
