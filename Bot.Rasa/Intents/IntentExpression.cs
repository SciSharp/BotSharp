using Bot.Rasa.Expressions;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Bot.Rasa.Intents
{
    [Table("Bot_IntentExpression")]
    public class IntentExpression : DbRecord, IDbRecord
    {
        public IntentExpression()
        {
            Entities = new List<EntitiyOfSpeech>();
        }

        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [Required]
        [MaxLength(128)]
        public String Text { get; set; }

        [ForeignKey("ExpressionId")]
        public List<EntitiyOfSpeech> Entities { get; set; }

        public bool IsExist(Database dc)
        {
            return dc.Table<IntentExpression>().Any(x => x.IntentId == IntentId && x.Text == Text);
        }
    }
}
