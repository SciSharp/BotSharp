using BotSharp.Platform.Models;
using BotSharp.Platform.Models.Intents;
using BotSharp.Platform.Models.MachineLearning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.Platform.OwnThink.Models
{
    public class AgentModel : AgentBase
    {
        public AgentModel()
        {
            
        }

        [JsonProperty("entity_types")]
        public List<EntityType> Entities { get; set; }
    }
}
