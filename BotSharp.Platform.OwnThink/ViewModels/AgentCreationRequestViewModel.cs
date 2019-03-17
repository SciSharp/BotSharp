using BotSharp.Platform.Models.Agents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.Platform.OwnThink.ViewModels
{
    public class AgentCreationRequestViewModel : AgentCreationRequestModel
    {
        [Required]
        public string AppId { get; set; }
    }
}
