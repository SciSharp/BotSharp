using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.A2A.Settings;

public class A2ASettings
{
    public bool Enabled { get; set; }
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public List<RemoteAgentConfig> Agents { get; set; } = new List<RemoteAgentConfig>();
}

public class RemoteAgentConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Endpoint { get; set; }
    public List<string> Capabilities { get; set; }
}
