using BotSharp.Voice;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Voice
{
    [Route("[controller]/[action]")]
    public class WakeupWordController : ControllerBase
    {
        [HttpGet]
        public void Record()
        {
            new AudioCapture().Start();
        }
    }
}
