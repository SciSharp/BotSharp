using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi
{
    [Route("[controller]/[action]")]
    public class AgentController : ControllerBase
    {
        /// <summary>
        /// Restore a agent from a uploaded zip file 
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public ActionResult Restore([FromRoute] String agentId)
        {
            return Ok();
        }
    }
}
