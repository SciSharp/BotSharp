using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    [JsonObject]
    public class QuestionMetadata
    {
        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("lang")]
        public string Language { get; set; }

        [JsonProperty("sessionId")]
        internal string SessionId { get; set; }

        [JsonProperty("entities")]
        public List<EntityType> Entities { get; set; }
    }
}
