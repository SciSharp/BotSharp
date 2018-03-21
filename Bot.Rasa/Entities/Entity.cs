using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Entities
{
    [Table("Bot_Entity")]
    public class Entity : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [Required]
        [MaxLength(64)]
        public String Name { get; set; }

        [NotMapped]
        public List<String> Values { get; set; }

        [ForeignKey("EntityId")]
        public List<EntityEntry> Entries { get; set; }
    }
}
