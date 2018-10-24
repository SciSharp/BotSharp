using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    /// <summary>
    /// Intent Table
    /// </summary>
    public class Intent
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [MaxLength(64)]
        public String Name { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        [ForeignKey("IntentId")]
        public List<IntentInputContext> Contexts { get; set; }

        [ForeignKey("IntentId")]
        public List<IntentEvent> Events { get; set; }
        
        /// <summary>
        /// Get input contexts hash
        /// </summary>
        [NotMapped]
        public String ContextHash { get; set; }

        [ForeignKey("IntentId")]
        public List<IntentExpression> UserSays { get; set; }

        [ForeignKey("IntentId")]
        public List<IntentResponse> Responses { get; set; }
    }
}
