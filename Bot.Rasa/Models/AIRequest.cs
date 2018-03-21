using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Models
{
    [JsonObject]
    public class AIRequest : QuestionMetadata
    {
        [JsonProperty("query")]
        public string[] Query { get; set; }

        [JsonProperty("confidence")]
        public float[] Confidence { get; set; }

        [JsonProperty("contexts")]
        public List<AIContext> Contexts { get; set; }

        [JsonProperty("resetContexts")]
        public bool? ResetContexts { get; set; }

        [JsonProperty("originalRequest")]
        public OriginalRequest OriginalRequest { get; set; }

        public AIRequest()
        {
            Contexts = new List<AIContext>();
        }

        public AIRequest(string text)
        {
            Query = new string[] { text };
            Confidence = new float[] { 1.0f };
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
