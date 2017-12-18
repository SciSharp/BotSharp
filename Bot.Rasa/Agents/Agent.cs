using Bot.Rasa.Intents;
using CustomEntityFoundation.Entities;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Agents
{
    public class RasaAgent : Entity, IDbRecord
    {
        [MaxLength(64)]
        public String Name { get; set; }

        [ForeignKey("AgentId")]
        public List<RasaIntent> Intents { get; set; }
    }
}
