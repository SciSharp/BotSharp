using BotSharp.Core.Agents;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class AgentImportHeader
    {
        public String Id { get; set; }
        public String Name { get; set; }
        public String Platform { get; set; }
        public String ClientAccessToken { get; set; }
        public String DeveloperAccessToken { get; set; }

        /// <summary>
        /// Integration with other social media platform
        /// </summary>
        public List<AgentIntegration> Integrations { get; set; }
    }
}
