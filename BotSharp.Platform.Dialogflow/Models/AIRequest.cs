using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class AIRequest : QuestionMetadata
    {
        public string[] Query { get; set; }

        public float[] Confidence { get; set; }

        public List<AIContext> Contexts { get; set; }

        public bool? ResetContexts { get; set; }

        public OriginalRequest OriginalRequest { get; set; }

        /// <summary>
        /// Agent directory
        /// </summary>
        public string AgentDir { get; set; }

        /// <summary>
        /// What model is used to predict.
        /// </summary>
        public string Model { get; set; }

        public AIRequest()
        {
            Contexts = new List<AIContext>();
        }

        public AIRequest(string text)
        {
            Query = new string[] { text };
            Confidence = new float[] { 1.0f };
            Contexts = new List<AIContext>();
        }

        public AIRequest(string text, RequestExtras requestExtras) : this(text)
        {
            if (requestExtras != null)
            {
                requestExtras.CopyTo(this);
            }
        }

    }
}
