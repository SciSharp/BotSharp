using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Conversations
{
    [Table("Bot_ConversationContext")]
    public class ConversationContext : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String ConversationId { get; set; }

        [Required]
        [MaxLength(64)]
        public String Context { get; set; }

        public int Lifespan { get; set; }
    }
}
