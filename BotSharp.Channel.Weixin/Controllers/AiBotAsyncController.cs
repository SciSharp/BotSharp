using BotSharp.Channel.Weixin.Models;
using BotSharp.Platform.Abstractions;
using BotSharp.Platform.Dialogflow.Models;
using BotSharp.Platform.Models.AiRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Senparc.CO2NET.HttpUtility;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MvcExtension;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BotSharp.Channel.Weixin.Controllers
{
    /// <summary>
    /// 纯Api
    /// </summary>
    [Route("aibot")]
    public class AiBotAsyncController : ControllerBase
    {
        readonly IConfiguration config;
        private IPlatformBuilder<AgentModel> builder;
        private IPlatformBuilder<AgentModel> nluPlatform = null;

        public AiBotAsyncController(IPlatformBuilder<AgentModel> platform, IConfiguration configuration)
        {
            config = configuration;
            builder = platform;
            nluPlatform = platform;
        }


        /// <summary>
        /// 最简化的处理流程
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Post(string retext,string sessionid)
        {
            var aIResponse = nluPlatform.TextRequest<AIResponseResult>(new AiRequest
            {
                Text = retext,
                AgentId = config.GetValue<string>("weixinChannel:agentId"),
                SessionId = sessionid,
            });

            return new ContentResult()
            {
                ContentType = "text/plain",
                StatusCode = 200,
                Content = aIResponse.Result.Fulfillment.Speech,
            };
        }
    }
}
