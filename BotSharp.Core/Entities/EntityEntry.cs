using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Entities
{
    [Table("Bot_EntityEntry")]
    public class EntityEntry : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String EntityId { get; set; }

        [MaxLength(128)]
        public String Value { get; set; }

        [NotMapped]
        public List<String> Synonyms { get; set; }
    }
}
