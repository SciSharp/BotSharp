using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Entities
{
    [Table("Bot_EntityItem")]
    public class EntityItem : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String EntityTypeId { get; set; }

        [MaxLength(128)]
        public String Value { get; set; }

        [NotMapped]
        public List<String> Synonyms { get; set; }
    }
}
