using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Intents
{
    [Table("Bot_IntentResponse")]
    public class IntentResponse : DbRecord, IDbRecord
    {
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
    }
}
