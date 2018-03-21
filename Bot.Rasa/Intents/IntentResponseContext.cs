using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Intents
{
    [Table("Bot_IntentResponseContext")]
    public class IntentResponseContext : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String IntentResponseId { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        public int Lifespan { get; set; }
    }
}
