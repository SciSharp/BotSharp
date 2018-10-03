using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.MachineLearning
{
    public class AgentMlConfig
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [Required]
        public decimal MinConfidence { get; set; }

        [Required]
        [MaxLength(64)]
        public string CustomClassifierMode { get; set; }

        [MaxLength(64)]
        public String Pipeline { get; set; }
    }
}
