using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.Dialogflow;
using BotSharp.Core.Models;
using BotSharp.NLP;
using BotSharp.RestApi.Integrations.FacebookMessenger;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.RestApi.Integrations
{
    [Route("v1/[controller]")]
    public class FacebookMessengerController : ControllerBase
    {
        [HttpGet("{agentId}")]
        public ActionResult Verify([FromRoute] string agentId)
        {
            var mode = Request.Query.ContainsKey("hub.mode") ? Request.Query["hub.mode"].ToString() : String.Empty;
            var token = Request.Query.ContainsKey("hub.verify_token") ? Request.Query["hub.verify_token"].ToString() : String.Empty;
            var challenge = Request.Query.ContainsKey("hub.challenge") ? Request.Query["hub.challenge"].ToString() : String.Empty;

            if (mode == "subscribe")
            {
                var dc = new DefaultDataContextLoader().GetDefaultDc();
                var config = dc.Table<AgentIntegration>().FirstOrDefault(x => x.AgentId == agentId && x.Platform == "Facebook Messenger");

                return config.VerifyToken == token ? Ok(challenge) : Ok(agentId);
            }

            return BadRequest();
        }

        [HttpPost("{agentId}")]
        public async Task<ActionResult> CallbackAsync([FromRoute] string agentId)
        {
            WebhookEvent body;
            IWebhookMessageBody response = null;

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var json = await reader.ReadToEndAsync();
                body = JsonConvert.DeserializeObject<WebhookEvent>(json);
            }

            body.Entry.ForEach(entry =>
            {
                entry.Messaging.ForEach(msg =>
                {
                    // received text message
                    if (msg.Message.ContainsKey("text"))
                    {
                        OnTextMessaged(agentId, new WebhookMessage<WebhookTextMessage>
                        {
                            Sender = msg.Sender,
                            Recipient = msg.Recipient,
                            Timestamp = msg.Timestamp,
                            Message = msg.Message.ToObject<WebhookTextMessage>()
                        });
                    }
                });

            });

            return Ok();
        }

        private void OnTextMessaged(string agentId, WebhookMessage<WebhookTextMessage> message)
        {
            Console.WriteLine($"OnTextMessaged: {message.Message.Text}");

            var ai = new ApiAi();
            var agent = ai.LoadAgent(agentId);
            ai.AiConfig = new AIConfiguration(agent.ClientAccessToken, SupportedLanguage.English) { AgentId = agentId };
            ai.AiConfig.SessionId = message.Sender.Id;
            var aiResponse = ai.TextRequest(new AIRequest { Query = new String[] { message.Message.Text } });

            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var config = dc.Table<AgentIntegration>().FirstOrDefault(x => x.AgentId == agentId && x.Platform == "Facebook Messenger");

            SendTextMessage(config.AccessToken, new WebhookMessage<WebhookTextMessage>
            {
                Recipient = message.Sender.ToObject<WebhookMessageRecipient>(),
                Message = new WebhookTextMessage
                {
                    Text = String.IsNullOrEmpty(aiResponse.Result.Fulfillment.Speech) ? aiResponse.Result.Action : aiResponse.Result.Fulfillment.Speech
                }
            });
        }

        private void SendTextMessage(string accessToken, WebhookMessage<WebhookTextMessage> body)
        {
            var client = new RestClient("https://graph.facebook.com");

            var rest = new RestRequest("v2.6/me/messages", Method.POST);
            string json = JsonConvert.SerializeObject(body,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });

            rest.AddParameter("application/json", json, ParameterType.RequestBody);
            rest.AddQueryParameter("access_token", accessToken);

            var response = client.Execute(rest);
        }
    }
}
