using CustomEntityFoundation.Entities;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Bot.Rasa.Agents
{
    public class RasaAgent : Entity, IDbRecord
    {
        [MaxLength(64)]
        public String Name { get; set; }
    }
}
