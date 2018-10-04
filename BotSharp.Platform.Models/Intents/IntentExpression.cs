using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    public class IntentExpression
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
    }
}
