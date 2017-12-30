using Bot.Rasa.Agents;
using Bot.Rasa.Intents;
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
        public static DbSet<Agent> Agent(this Database dc)
        {
            return dc.Table<Agent>();
        }

        public static DbSet<Intent> Intent(this Database dc)
        {
            return dc.Table<Intent>();
        }

        public static DbSet<IntentExpression> IntentExpression(this Database dc)
        {
            return dc.Table<IntentExpression>();
        }
    }
}
