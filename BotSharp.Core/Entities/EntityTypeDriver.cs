using BotSharp.Core.Agents;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Entities
{
    public static class EntityTypeDriver
    {
        public static string CreateEntityType(this Agent agent, Database dc, EntityType entityType)
        {
            if (dc.Table<EntityType>().Any(x => x.Name == entityType.Name && x.AgentId == agent.Id)) return agent.Id;

            dc.Table<EntityType>().Add(entityType);

            return entityType.Id;
        }

        public static void UpdateEntityType(this Agent agent, Database dc, String entityTypeId, EntityType entityType)
        {
            var existedEntityType = dc.Table<EntityType>().Find(entityTypeId);
            if (entityType == null) return;

            existedEntityType.Name = entityType.Name;
            existedEntityType.Description = entityType.Description;
            existedEntityType.Color = entityType.Color;
            existedEntityType.IsEnum = entityType.IsEnum;
            existedEntityType.UpdatedTime = DateTime.UtcNow;
        }

        public static void DeleteEntityType(this Agent agent, Database dc, String entityTypeId)
        {
            var entityType = dc.Table<EntityType>().FirstOrDefault(x => x.Id == entityTypeId);
            if (entityType == null) return;

            dc.Table<EntityType>().Remove(entityType);
        }
    }
}
