using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.Contexts
{
    public class AIContext
    {
        public string Name { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Lifespan of the context measured in requests````
        /// </summary>
        public int Lifespan { get; set; }

        public AIContext()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
}
