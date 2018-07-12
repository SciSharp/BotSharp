using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Intents
{
    [Table("Bot_IntentEvent")]
    public class IntentEvent : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [MaxLength(64)]
        public String Name { get; set; }
    }
}
