using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailReader.Settings;

public class EmailReaderSettings
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IMAPServer { get; set; } = string.Empty;
    public int IMAPPort { get; set; }
}
