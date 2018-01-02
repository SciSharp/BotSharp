using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.RestApi
{
    public class EntityItemController : EssentialController
    {
        [HttpPost]
        public IActionResult CreateEntity()
        {
            return Ok();
        }
    }
}
