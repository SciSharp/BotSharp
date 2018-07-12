using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Agents
{
    [Table("Bot_AgentMlConfig")]
    public class AgentMlConfig : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [Required]
        public decimal MinConfidence { get; set; }

        [Required]
        [MaxLength(64)]
        public string CustomClassifierMode { get; set; }

        [MaxLength(64)]
        public String Pipeline { get; set; }
    }
}
