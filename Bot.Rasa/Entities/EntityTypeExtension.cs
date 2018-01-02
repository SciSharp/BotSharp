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
        public static string CreateEntityType(this Agent agent, Database dc, EntityType entityType)
        {
            if (dc.Table<EntityType>().Any(x => x.Name == entityType.Name && x.AgentId == agent.Id)) return agent.Id;

            dc.Table<EntityType>().Add(entityType);

            return entityType.Id;
        }

        public static void DeleteEntityType(this Agent agent, Database dc, String entityTypeId)
        {
            var entityType = dc.Table<EntityType>().FirstOrDefault(x => x.Id == entityTypeId);
            if (entityType == null) return;

            dc.Table<EntityType>().Remove(entityType);
        }
    }
}
