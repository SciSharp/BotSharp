using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Sessions
{
    [Table("Bot_SessionContext")]
    public class SessionContext : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String SessionId { get; set; }

        [Required]
        [MaxLength(64)]
        public String Context { get; set; }

        public int Lifespan { get; set; }
    }
}
