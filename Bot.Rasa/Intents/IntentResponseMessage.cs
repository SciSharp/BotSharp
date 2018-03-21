using Bot.Rasa.Models;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Intents
{
    [Table("Bot_IntentResponseMessage")]
    public class IntentResponseMessage : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String IntentResponseId { get; set; }

        public AIResponseMessageType Type { get; set; }

        [Required]
        [MaxLength(3)]
        public String Lang { get; set; }

        /// <summary>
        /// json list data
        /// </summary>
        [MaxLength(1024)]
        public String Speech { get; set; }

        /// <summary>
        /// custom json payload
        /// </summary>
        [MaxLength(1024)]
        public String Payload { get; set; }
    }
}
