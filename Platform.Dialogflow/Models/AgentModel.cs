using BotSharp.Platform.Models;
using BotSharp.Platform.Models.Intents;
using BotSharp.Platform.Models.MachineLearning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Platform.Dialogflow.Models
{
    public class AgentModel : AgentBase
    {
        public AgentModel()
        {
            
        }

        public Boolean Published { get; set; }

        /// <summary>
        /// Only access text/ audio rquest
        /// </summary>
        [StringLength(32)]
        public String ClientAccessToken { get; set; }

        /// <summary>
        /// Developer can access more APIs
        /// </summary>
        [StringLength(32)]
        public String DeveloperAccessToken { get; set; }

        public List<Intent> Intents { get; set; }

        [JsonProperty("entity_types")]
        public List<EntityType> Entities { get; set; }

        public String Birthday
        {
            get
            {
                return CreatedDate.ToShortDateString();
            }
        }

        public Boolean IsSkillSet { get; set; }

        public AgentMlConfig MlConfig { get; set; }

        public List<AgentIntegration> Integrations { get; set; }
    }
}
