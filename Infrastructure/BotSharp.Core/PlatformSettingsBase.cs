using BotSharp.Platform.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core
{
    public class PlatformSettingsBase : IPlatformSettings
    {
        /// <summary>
        /// Set default settings
        /// </summary>
        public PlatformSettingsBase()
        {
            BotEngine = "BotSharpNLU";
            AgentStorage = "AgentStorageInFile";
            ContextStorage = "ContextStorageInFile";
        }

        public string BotEngine { get; set; }

        public string ContextStorage { get; set; }

        public string AgentStorage { get; set; }
    }

}
