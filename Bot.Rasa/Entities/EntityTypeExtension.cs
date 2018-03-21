using Bot.Rasa.Agents;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Rasa.Entities
{
    public static class EntityTypeExtension
    {
        public static string CreateEntityType(this Agent agent, Database dc, Entity entityType)
        {
            if (dc.Table<Entity>().Any(x => x.Name == entityType.Name && x.AgentId == agent.Id)) return agent.Id;

            dc.Table<Entity>().Add(entityType);

            return entityType.Id;
        }

        public static void DeleteEntityType(this Agent agent, Database dc, String entityTypeId)
        {
            var entityType = dc.Table<Entity>().FirstOrDefault(x => x.Id == entityTypeId);
            if (entityType == null) return;

            dc.Table<Entity>().Remove(entityType);
        }
    }
}
