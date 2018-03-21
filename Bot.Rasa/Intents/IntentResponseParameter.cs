using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Intents
{
    [Table("Bot_IntentResponseParameter")]
    public class IntentResponseParameter : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String IntentResponseId { get; set; }
        public bool Required { get; set; }
        public string DataType { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsList { get; set; }
    }
}
