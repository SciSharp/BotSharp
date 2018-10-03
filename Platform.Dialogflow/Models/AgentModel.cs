using BotSharp.Core.Agents;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.Intents;
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
            CreatedDate = DateTime.UtcNow;
        }

        [Required]
        [MaxLength(64)]
        public String Name { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        public Boolean Published { get; set; }

        [Required]
        [MaxLength(5)]
        public String Language { get; set; }

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

        [Required]
        public DateTime CreatedDate { get; set; }

        public Boolean IsSkillSet { get; set; }

        public AgentMlConfig MlConfig { get; set; }

        public TrainingCorpus Corpus { get; set; }

        public List<AgentIntegration> Integrations { get; set; }
    }
}
