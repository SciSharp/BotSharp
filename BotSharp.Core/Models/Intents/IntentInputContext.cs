using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    public class IntentInputContext
    {
        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [Required]
        [MaxLength(64)]
        public string Name { get; set; }
    }
}
