using BotSharp.Core.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Dialogflow
{
    [JsonObject]
    public class DialogflowEntityEntry
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        public List<String> RawSynonyms { get; set; }

        public List<EntrySynonym> Synonyms { get; set; }

        public DialogflowEntityEntry()
        {
        }

        public DialogflowEntityEntry(string value, List<string> synonyms)
        {
            this.Value = value;
            this.RawSynonyms = synonyms;
        }

        public DialogflowEntityEntry(string value, string[] synonyms) : this(value, new List<string>(synonyms))
        {

        }
    }
}
