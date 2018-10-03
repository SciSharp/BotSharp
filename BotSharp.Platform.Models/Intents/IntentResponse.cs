using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    public class IntentResponse
    {
        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [MaxLength(128)]
        public String Action { get; set; }

        public Boolean ResetContexts { get; set; }

        [ForeignKey("IntentResponseId")]
        public List<IntentResponseContext> Contexts { get; set; }

        [ForeignKey("IntentResponseId")]
        public List<IntentResponseParameter> Parameters { get; set; }

        [ForeignKey("IntentResponseId")]
        public List<IntentResponseMessage> Messages { get; set; }

        [NotMapped]
        public string IntentName { get; set; }
    }
}
