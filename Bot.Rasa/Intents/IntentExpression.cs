using CustomEntityFoundation;
using CustomEntityFoundation.Entities;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Bot.Rasa.Intents
{
    public class RasaIntentExpression : Entity, IDbRecord
    {
        [Required]
        [StringLength(36)]
        public String IntentId { get; set; }

        [Required]
        [MaxLength(128)]
        public String Text { get; set; }

        public override bool IsExist<T>(EntityDbContext dc)
        {
            return dc.Table<RasaIntentExpression>().Any(x => x.IntentId == IntentId && x.Text == Text);
        }
    }
}
