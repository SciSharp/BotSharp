using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.Platform.Models.Agents
{
    public class AgentCreationRequestModel
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
