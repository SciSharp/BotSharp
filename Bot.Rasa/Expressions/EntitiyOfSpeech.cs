using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Expressions
{
    [Table("Bot_EntityOfSpeech")]
    public class EntitiyOfSpeech : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String ExpressionId { get; set; }

        public int Start { get; set; }

        [Required]
        [MaxLength(128)]
        public String Value { get; set; }

        [Required]
        [MaxLength(64)]
        public String Entity { get; set; }
    }
}
