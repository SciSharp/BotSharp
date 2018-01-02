using Bot.Rasa.Agents;
using Bot.Rasa.Entities;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.RestApi
{
    public class EntityTypeController : EssentialController
    {
        [HttpPost]
        public string CreateType([FromBody] EntityType entityType)
        {
            var agent = dc.Table<Agent>().Find(entityType.AgentId);
            dc.DbTran(() => agent.CreateEntityType(dc, entityType));

            return entityType.Id;
        }

        [HttpDelete("{agentId}/{entityTypeId}")]
        public IActionResult DeleteType([FromRoute] String agentId, [FromRoute] String entityTypeId)
        {
            var agent = dc.Table<Agent>().Find(agentId);
            dc.DbTran(() => agent.DeleteEntityType(dc, entityTypeId));

            return Ok();
        }
    }
}
