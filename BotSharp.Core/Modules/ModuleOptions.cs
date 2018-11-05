using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Modules
{
    /// <summary>
    /// Module configuration .
    /// </summary>
    public class ModuleOptions
    {
        public ModuleOptions()
        {
            Path = String.Empty;
        }

        /// <summary>
        /// Module name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Module type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Module path
        /// </summary>
        public string Path { get; set; }
    }
}
