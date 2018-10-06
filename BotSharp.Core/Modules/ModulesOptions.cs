using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Modules
{
    /// <summary>
    /// Module Host configuration.
    /// </summary>
    public class ModulesOptions
    {
        /// <summary>
        /// Module base directory
        /// </summary>
        public String ModuleBasePath { get; set; }

        /// <summary>
        /// List of module configurations.
        /// </summary>
        public List<ModuleOptions> Modules { get; set; }
    }
}
