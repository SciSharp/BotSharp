using Bot.Rasa.Agents;
using CustomEntityFoundation;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Rasa
{
    public static class EntityDbContextExtension
    {
        public static DbSet<RasaAgent> Agent(this EntityDbContext dc)
        {
            return dc.Table<RasaAgent>();
        }
    }
}
