using BotSharp.Core.Engines;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Ontology
{
    [Route("v1/[controller]/[action]")]
    public class EntityController : ControllerBase
    {
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public EntityController(IBotPlatform platform)
        {
            _platform = platform;
        }
    }
}
