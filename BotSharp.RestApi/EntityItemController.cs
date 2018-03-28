using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.RestApi
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
