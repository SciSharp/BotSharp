using BotSharp.Core.Agents;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Entities
{
    public static class EntityEntryDriver
    {
        public static void CreateEntity(this Agent agent, Database dc, EntityType entity)
        {
            dc.Table<EntityType>().Add(entity);
        }

        public static void DeleteEntity(this Agent agent, Database dc, string entityId)
        {
            var entity = dc.Table<EntityType>().Find(entityId);
            dc.Table<EntityType>().Remove(entity);
        }

        public static void UpdateEntity(this Agent agent, Database dc, EntityType entity)
        {
            var oldEntity = dc.Table<EntityType>().Find(entity.Id);
            oldEntity.Name = entity.Name;
        }
    }
}
