using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Langfuse;

/// <summary>
/// Langfuse Settings
/// </summary>
public class LangfuseSettings
{
    public string SecretKey { get; set; }

    public string PublicKey { get; set; }

    public string Host { get; set; }
}
