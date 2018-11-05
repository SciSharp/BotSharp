using BotSharp.Channel.Weixin.Models;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Dialogflow;
using BotSharp.Platform.Dialogflow.Models;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Senparc.CO2NET;
using Senparc.CO2NET.HttpUtility;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MvcExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Channel.Weixin.Controllers
{
    /// <summary>
    /// 此Controller为异步Controller（Action），使用异步线程处理并发请求。
    /// 为了方便演示，此Controller中没有加入多余的日志记录等示例，保持了最简单的Controller写法。日志等其他操作可以参考WeixinController.cs。
    /// 提示：异步Controller并不是在任何情况下都能提升效率（响应时间），当请求量非常小的时候反而会增加一定的开销。
    /// </summary>
    [Route("weixin")]
    public class WeixinAsyncController : ControllerBase
    {
        readonly Func<string> _getRandomFileName = () => DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_Async_" + Guid.NewGuid().ToString("n").Substring(0, 6);

        readonly IConfiguration config;
        private IPlatformBuilder<AgentModel> builder;

        public WeixinAsyncController(IPlatformBuilder<AgentModel> platform, IConfiguration configuration)
        {
            config = configuration;
            builder = platform;
        }

        [HttpGet]
        public Task<ActionResult> Get(string signature, string timestamp, string nonce, string echostr)
        {
            var token = config.GetValue<string>("weixinChannel:token");
            return Task.Factory.StartNew(() =>
            {
                if (CheckSignature.Check(signature, timestamp, nonce, token))
                {
                    return echostr; //返回随机字符串则表示验证通过
                }
                else
                {
                    return "failed:" + signature + "," + Senparc.Weixin.MP.CheckSignature.GetSignature(timestamp, nonce, token) + "。" +
                        "如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。";
                }
            }).ContinueWith<ActionResult>(task => Content(task.Result));
        }

        public CustomMessageHandler MessageHandler = null;//开放出MessageHandler是为了做单元测试，实际使用过程中不需要

        /// <summary>
        /// 最简化的处理流程
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Post(PostModel postModel)
        {
            var token = config.GetValue<string>("weixinChannel:token");

            if (!CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, token))
            {
                return new WeixinResult("参数错误！");
            }

            postModel.Token = token;
            postModel.EncodingAESKey = config.GetValue<string>("weixinChannel:encodingAESKey"); 
            postModel.AppId = config.GetValue<string>("weixinChannel:appId"); 

            var messageHandler = new CustomMessageHandler(builder, config, Request.GetRequestMemoryStream(), postModel, 10);

            messageHandler.DefaultMessageHandlerAsyncEvent = Senparc.NeuChar.MessageHandlers.DefaultMessageHandlerAsyncEvent.SelfSynicMethod;//没有重写的异步方法将默认尝试调用同步方法中的代码（为了偷懒）

            #region 设置消息去重

            /* 如果需要添加消息去重功能，只需打开OmitRepeatedMessage功能，SDK会自动处理。
             * 收到重复消息通常是因为微信服务器没有及时收到响应，会持续发送2-5条不等的相同内容的RequestMessage*/
            messageHandler.OmitRepeatedMessage = true;//默认已经开启，此处仅作为演示，也可以设置为false在本次请求中停用此功能

            #endregion

            #region 记录 Request 日志

            var logPath = Server.GetMapPath(string.Format("~/App_Data/MP/{0}/", DateTime.Now.ToString("yyyy-MM-dd")));
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            //测试时可开启此记录，帮助跟踪数据，使用前请确保App_Data文件夹存在，且有读写权限。
            messageHandler.RequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_{1}_{2}.txt", _getRandomFileName(),
                messageHandler.RequestMessage.FromUserName,
                messageHandler.RequestMessage.MsgType)));
            if (messageHandler.UsingEcryptMessage)
            {
                messageHandler.EcryptRequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_Ecrypt_{1}_{2}.txt", _getRandomFileName(),
                    messageHandler.RequestMessage.FromUserName,
                    messageHandler.RequestMessage.MsgType)));
            }

            #endregion

            await messageHandler.ExecuteAsync(); //执行微信处理过程

            #region 记录 Response 日志

            //测试时可开启，帮助跟踪数据

            //if (messageHandler.ResponseDocument == null)
            //{
            //    throw new Exception(messageHandler.RequestDocument.ToString());
            //}
            if (messageHandler.ResponseDocument != null)
            {
                messageHandler.ResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_{1}_{2}.txt", _getRandomFileName(),
                    messageHandler.ResponseMessage.ToUserName,
                    messageHandler.ResponseMessage.MsgType)));
            }

            if (messageHandler.UsingEcryptMessage && messageHandler.FinalResponseDocument != null)
            {
                //记录加密后的响应信息
                messageHandler.FinalResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_Final_{1}_{2}.txt", _getRandomFileName(),
                    messageHandler.ResponseMessage.ToUserName,
                    messageHandler.ResponseMessage.MsgType)));
            }

            #endregion

            MessageHandler = messageHandler;//开放出MessageHandler是为了做单元测试，实际使用过程中不需要

            return new FixWeixinBugWeixinResult(messageHandler);
        }
    }
}
