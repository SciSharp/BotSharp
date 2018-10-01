using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Articulate.Controllers
{
#if ARTICULATE
    [Route("[controller]")]
    public class ParseControllercs : ControllerBase
    {
        [HttpGet("/agent/{agentId}/converse")]
        public void ParseText([FromRoute] string agentId, [FromQuery] string text, [FromQuery] string sessionId)
        {
            
        }
    }
#endif
}
