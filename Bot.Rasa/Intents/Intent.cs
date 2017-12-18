using CustomEntityFoundation.Entities;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Intents
{
    public class RasaIntent : Entity, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [MaxLength(32)]
        public String Name { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        [ForeignKey("IntentId")]
        public List<RasaIntentExpression> Expressions { get; set; }
    }
}
