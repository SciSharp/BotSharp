using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    public class IntentEvent
    {
        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [MaxLength(64)]
        public String Name { get; set; }
    }
}
