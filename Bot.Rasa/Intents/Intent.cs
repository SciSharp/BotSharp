using CustomEntityFoundation.Entities;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bot.Rasa.Intents
{
    public class RasaIntent : Entity, IDbRecord
    {
        public String Name { get; set; }
    }
}
