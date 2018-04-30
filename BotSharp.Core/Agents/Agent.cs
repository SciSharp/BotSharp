using BotSharp.Core.Entities;
using BotSharp.Core.Intents;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Agents
{
    [Table("Bot_Agent")]
    public class Agent : DbRecord, IDbRecord
    {
        [MaxLength(64)]
        public String Name { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        public Boolean Published { get; set; }

        [MaxLength(3)]
        public String Language { get; set; }

        /// <summary>
        /// Only access text/ audio rquest
        /// </summary>
        [StringLength(32)]
        public String ClientAccessToken { get; set; }

        /// <summary>
        /// Developer can access more APIs
        /// </summary>
        [StringLength(32)]
        public String DeveloperAccessToken { get; set; }

        [ForeignKey("AgentId")]
        public List<Intent> Intents { get; set; }

        [ForeignKey("AgentId")]
        [JsonProperty("entity_types")]
        public List<Entity> Entities { get; set; }
    }
}
