using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Dialogflow
{
    [JsonObject]
    public class DialogflowEntity
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("entries")]
        public List<DialogflowEntityEntry> Entries { get; set; }

        public DialogflowEntity()
        {
        }

        public DialogflowEntity(string name)
        {
            this.Name = name;
        }

        public DialogflowEntity(string name, List<DialogflowEntityEntry> entries)
        {
            this.Name = name;
            this.Entries = entries;
        }

        public void AddEntry(DialogflowEntityEntry entry)
        {
            if (Entries == null)
            {
                Entries = new List<DialogflowEntityEntry>();
            }

            Entries.Add(entry);
        }
    }
}
