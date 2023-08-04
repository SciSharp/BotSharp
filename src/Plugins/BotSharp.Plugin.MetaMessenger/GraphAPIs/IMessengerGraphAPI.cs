using BotSharp.Plugin.MetaMessenger.MessagingModels;
using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MetaMessenger.GraphAPIs;

public interface IMessengerGraphAPI
{
    /// <summary>
    /// https://developers.facebook.com/docs/messenger-platform/reference/send-api
    /// </summary>
    /// <example>
    /// /v17.0/104435566084134/messages?recipient={'id':'6610279455689235'}&messaging_type=RESPONSE&message={'text':'hello,world'}&access_token=
    /// </example>
    [Post("/{apiVer}/{pageId}/messages")]
    Task<SendingMessageResponse> SendMessage(string apiVer, string pageId, [Query] SendingMessageRequest request);
}
