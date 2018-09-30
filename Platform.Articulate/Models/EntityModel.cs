using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Models
{
    public class EntityModel : EntityBase
    {
        public string Regex { get; set; }

        public string Agent { get; set; }

        public string EntityName { get; set; }

        public string Type { get; set; }

        public string UiColor { get; set; }

        public List<EntitySynonymModel> Examples { get; set; }
    }
}
