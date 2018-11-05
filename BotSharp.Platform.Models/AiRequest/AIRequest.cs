using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.AiRequest
{
    public class AiRequest
    {
        public AiRequest()
        {
            Contexts = new List<string>();
        }

        public string AgentId { get; set; }

        public string Text { get; set; }

        public string SessionId { get; set; }

        public List<String> Contexts { get; set; }

        public bool ResetContexts { get; set; }

        /// <summary>
        /// What model is used to predict.
        /// It's optional.
        /// </summary>
        public string Model { get; set; }

        public string AgentDir { get; set; }
    }
}
