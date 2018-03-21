using Bot.Rasa.Entities;
using Bot.Rasa.Intents;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Agents
{
    [Table("Bot_Agent")]
    public class Agent : DbRecord, IDbRecord
    {
        [MaxLength(64)]
        public String Name { get; set; }

        public String Description { get; set; }

        public Boolean Published { get; set; }

        public String Language { get; set; }

        [ForeignKey("AgentId")]
        public List<Intent> Intents { get; set; }

        [ForeignKey("AgentId")]
        [JsonProperty("entity_types")]
        public List<Entity> Entities { get; set; }
    }
}
