using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.AiResponse
{
    public class ResponseParameterPrompt
    {
        [Required]
        [StringLength(36)]
        public String ResponseParameterId { get; set; }

        [MaxLength(256)]
        public string Prompt { get; set; }
    }
}
