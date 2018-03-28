using BotSharp.Core.Expressions;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Intents
{
    [Table("Bot_IntentExpression")]
    public class IntentExpression : DbRecord, IDbRecord
    {
        public IntentExpression()
        {
            Data = new List<IntentExpressionPart>();
        }

        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [ForeignKey("ExpressionId")]
        public List<IntentExpressionPart> Data { get; set; }

        public bool IsExist(Database dc)
        {
            return dc.Table<IntentExpression>().Any(x => x.IntentId == IntentId);
        }
    }
}
