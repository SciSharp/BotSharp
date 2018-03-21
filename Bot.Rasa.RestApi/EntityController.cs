using Bot.Rasa.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.RestApi
{
    public class EntityController : EssentialController
    {
        [HttpPost]
        public string CreateEntity([FromBody] Entity entity)
        {
            return entity.Id;
        }
    }
}
