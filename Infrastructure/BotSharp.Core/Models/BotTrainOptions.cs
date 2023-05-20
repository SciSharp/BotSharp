using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models
{
    public class BotTrainOptions
    {
        /// <summary>
        /// Agent data direcotry
        /// </summary>
        public string AgentDir { get; set; }

        /// <summary>
        /// Model Name
        /// </summary>
        public string Model { get; set; }
    }
}
