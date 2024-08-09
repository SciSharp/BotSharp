using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace BotSharp.Plugin.Twilio.Controllers;

[AllowAnonymous]
[Route("[controller]")]
public class TwilioVoiceController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;

    public TwilioVoiceController(TwilioSetting settings, IServiceProvider services)
    {
        _settings = settings;
        _services = services;
    }

    [Authorize]
    [HttpGet("/twilio/token")]
    public Token GetAccessToken()
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var accessToken = twilio.GetAccessToken();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
    }

    [HttpPost("/twilio/voice/welcome")]
    public async Task<TwiMLResult> StartConversation(VoiceRequest request)
    {
        string sessionId = $"TwilioVoice_{request.CallSid}";
        var twilio = _services.GetRequiredService<TwilioService>();
        var response = twilio.ReturnInstructions("Hello, how may I help you?");
        return TwiML(response);
    }

    [HttpPost("/twilio/voice/{agentId}")]
    public async Task<TwiMLResult> ReceivedVoiceMessage([FromRoute] string agentId, VoiceRequest input)
    {
        string sessionId = $"TwilioVoice_{input.CallSid}";

        var inputMsg = new RoleDialogModel(AgentRole.User, input.SpeechResult);
        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(sessionId, inputMsg.MessageId);

        conv.SetConversationId(sessionId, new List<MessageState>
        {
            new MessageState("channel", ConversationChannel.Phone),
            new MessageState("calling_phone", input.DialCallSid)
        });

        var twilio = _services.GetRequiredService<TwilioService>();
        VoiceResponse response = default;

        var result = await conv.SendMessage(agentId,
            inputMsg,
            replyMessage: null,
            async msg =>
            {
                response = twilio.ReturnInstructions(msg.Content);
                if (msg.FunctionName == "conversation_end")
                {
                    response = twilio.HangUp(msg.Content);
                }
            }, async functionExecuting =>
            {
            }, async functionExecuted =>
            {
            });

        return TwiML(response);
    }


    [HttpPost("start")]
    public TwiMLResult InitiateConversation(VoiceRequest request)
    {
        if (request?.CallSid == null) throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        string sessionId = $"TwilioVoice_{request.CallSid}";
        var twilio = _services.GetRequiredService<TwilioService>();
        var url = $"twiliovoice/{sessionId}/send/0";
        var response = twilio.ReturnInstructions("twilio/welcome.mp3", url, false);
        return TwiML(response);
    }

    [HttpPost("{sessionId}/send/{seqNum}")]
    public async Task<TwiMLResult> SendCallerMessage([FromRoute] string sessionId, [FromRoute] int seqNum, VoiceRequest request)
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var messageQueue = _services.GetRequiredService<TwilioMessageQueue>();
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var url = $"twiliovoice/{sessionId}/reply/{seqNum}";
        var messages = await sessionManager.RetrieveStagedCallerMessagesAsync(sessionId, seqNum);
        if (!string.IsNullOrWhiteSpace(request.SpeechResult))
        {
            messages.Add(request.SpeechResult);
        }
        var messageContent = string.Join("\r\n", messages);
        VoiceResponse response;
        if (!string.IsNullOrWhiteSpace(messageContent))
        {
            var callerMessage = new CallerMessage()
            {
                SessionId = sessionId,
                SeqNumber = seqNum,
                Content = messageContent,
                From = request.From
            };
            await messageQueue.EnqueueAsync(callerMessage);
            response = twilio.ReturnInstructions("twilio/holdon.mp3", url, true);
        }
        else
        {
            response = twilio.HangUp("twilio/holdon.mp3");
        }
        return TwiML(response);
    }

    [HttpPost("{sessionId}/reply/{seqNum}")]
    public async Task<TwiMLResult> ReplyCallerMessage([FromRoute] string sessionId, [FromRoute] int seqNum, VoiceRequest request)
    {
        var nextSeqNum = seqNum + 1;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var twilio = _services.GetRequiredService<TwilioService>();
        if (request.SpeechResult != null)
        {
            await sessionManager.StageCallerMessageAsync(sessionId, nextSeqNum, request.SpeechResult);
        }
        var reply = await sessionManager.GetAssistantReplyAsync(sessionId, seqNum);
        VoiceResponse response;
        if (string.IsNullOrEmpty(reply))
        {
            response = twilio.ReturnInstructions(null, $"twiliovoice/{sessionId}/reply/{seqNum}", true);
        }
        else
        {

            var textToSpeechService = CompletionProvider.GetTextToSpeech(_services, "openai", "tts-1");
            var fileService = _services.GetRequiredService<IFileStorageService>();
            var data = await textToSpeechService.GenerateSpeechFromTextAsync(reply);
            var fileName = $"{seqNum}.mp3";
            await fileService.SaveSpeechFileAsync(sessionId, fileName, data);
            response = twilio.ReturnInstructions($"twiliovoice/speeches/{sessionId}/{fileName}", $"twiliovoice/{sessionId}/send/{nextSeqNum}", true);
        }
        return TwiML(response);
    }

    [HttpGet("speeches/{conversationId}/{fileName}")]
    public async Task<FileContentResult> RetrieveSpeechFile([FromRoute] string conversationId, [FromRoute] string fileName)
    {
        var fileService = _services.GetRequiredService<IFileStorageService>();
        var data = await fileService.RetrieveSpeechFileAsync(conversationId, fileName);
        var result = new FileContentResult(data.ToArray(), "audio/mpeg");
        result.FileDownloadName = fileName;
        return result;
    }
}
