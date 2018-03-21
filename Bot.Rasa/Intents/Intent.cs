using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Intents
{
    /// <summary>
    /// Intent Table
    /// </summary>
    [Table("Bot_Intent")]
    public class Intent : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [MaxLength(32)]
        public String Name { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        [ForeignKey("IntentId")]
        public List<IntentInputContext> Contexts { get; set; }

        [ForeignKey("IntentId")]
        public List<IntentExpression> UserSays { get; set; }

        [ForeignKey("IntentId")]
        public List<IntentResponse> Responses { get; set; }
    }
}
