using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.Platform.Models
{
    public abstract class IntentBase
    {
        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        /// <summary>
        /// Name of chatbot
        /// </summary>
        [Required]
        [MaxLength(64)]
        public String Name { get; set; }
    }
}
