using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Intents
{
    [Table("Bot_ContextModelMapping")]
    public class ContextModelMapping : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [Required]
        [StringLength(32)]
        public string ContextId { get; set; }

        [Required]
        [StringLength(21)]
        public string ModelName { get; set; }
    }
}
