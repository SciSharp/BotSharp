using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models
{
    /// <summary>
    /// Standard agent data structure
    /// All other platform agent has to align with this standard data structure.
    /// </summary>
    public class StandardAgent : AgentBase
    {
        public StandardAgent()
        {
            CreatedDate = DateTime.UtcNow;
            Entities = new List<EntityBase>();
            Intents = new List<IntentBase>();
        }

        /// <summary>
        /// Is the chatbot public or private
        /// </summary>
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

        public List<IntentBase> Intents { get; set; }

        public List<EntityBase> Entities { get; set; }

        public String Birthday
        {
            get
            {
                return CreatedDate.ToShortDateString();
            }
        }

        public DateTime CreatedDate { get; set; }
    }
}
