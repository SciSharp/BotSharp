using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    public class IntentResponseMessage
    {
        [Required]
        [StringLength(36)]
        public String IntentResponseId { get; set; }

        public AIResponseMessageType Type { get; set; }

        /// <summary>
        /// Platform like: facebook, slack
        /// </summary>
        public String Platform { get; set; }

        /// <summary>
        /// json list data
        /// </summary>
        [MaxLength(1024)]
        public String Speech { get; set; }

        /// <summary>
        /// custom json payload
        /// </summary>
        [MaxLength(1024)]
        public String PayloadJson { get; set; }

        public String CardJson { get; set; }

        [NotMapped]
        public Object Payload { get; set; }

        public DateTime UpdatedTime { get; set; }
    }
}
