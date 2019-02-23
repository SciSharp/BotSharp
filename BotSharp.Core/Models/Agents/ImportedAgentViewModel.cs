using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.Agents
{
    public class ImportedAgentViewModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string ClientAccessToken { get; set; }
    }
}
