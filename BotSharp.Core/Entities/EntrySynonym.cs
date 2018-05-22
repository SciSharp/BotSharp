using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Entities
{
    [Table("Bot_EntityEntrySynonym")]
    public class EntrySynonym : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String EntityEntryId { get; set; }

        [MaxLength(64)]
        public String Synonym { get; set; }
    }
}
