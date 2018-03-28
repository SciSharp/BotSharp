using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Expressions
{
    [Table("Bot_IntentExpressionPart")]
    public class IntentExpressionPart : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String ExpressionId { get; set; }

        [Required]
        [MaxLength(128)]
        public String Text { get; set; }

        [MaxLength(64)]
        public String Alias { get; set; }

        [MaxLength(64)]
        public String Meta { get; set; }

        public Boolean UserDefined { get; set; }
    }
}
