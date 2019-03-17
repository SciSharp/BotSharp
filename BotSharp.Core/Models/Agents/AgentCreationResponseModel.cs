using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.Agents
{
    public class AgentCreationResponseModel
    {
        public string AgentId { get; set; }

        public string Name { get; set; }

        public string ClientAccessToken { get; set; }
    }
}
