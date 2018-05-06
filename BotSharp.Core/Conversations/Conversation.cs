using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Conversations
{
    [Table("Bot_Conversation")]
    public class Conversation : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [Required]
        [StringLength(36)]
        public String UserId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }
    }
}
