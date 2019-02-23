using BotSharp.Platform.Models.Intents;
using BotSharp.Platform.Models.MachineLearning;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.Platform.Models
{
    public abstract class AgentBase
    {
        public AgentBase()
        {
            CreatedDate = DateTime.UtcNow;
            Intents = new List<Intent>();
        }

        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        /// <summary>
        /// Name of chatbot
        /// </summary>
        [Required]
        [MaxLength(64)]
        public virtual String Name { get; set; }

        /// <summary>
        /// Description of chatbot
        /// </summary>
        [MaxLength(256)]
        public String Description { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        [MaxLength(5)]
        public String Language { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public TrainingCorpus Corpus { get; set; }

        public AgentMlConfig MlConfig { get; set; }

        public List<AgentIntegration> Integrations { get; set; }

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

        public String Birthday
        {
            get
            {
                return CreatedDate.ToShortDateString();
            }
        }
    }
}
