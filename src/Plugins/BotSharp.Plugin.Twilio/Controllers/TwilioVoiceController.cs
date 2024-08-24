using BotSharp.Abstraction.Files;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioVoiceController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _context;

    public TwilioVoiceController(TwilioSetting settings, IServiceProvider services, IHttpContextAccessor context)
    {
        _settings = settings;
        _services = services;
        _context = context;
    }

    /// <summary>
    /// https://github.com/twilio-labs/twilio-aspnet?tab=readme-ov-file#validate-twilio-http-requests
    /// </summary>
    /// <param name="request"></param>
    /// <param name="states"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [ValidateRequest]
    [HttpPost("twilio/voice/welcome")]
    public TwiMLResult InitiateConversation(VoiceRequest request, [FromQuery] string states)
    {
        if (request?.CallSid == null) throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        string conversationId = $"TwilioVoice_{request.CallSid}";
        var twilio = _services.GetRequiredService<TwilioService>();
        var url = $"twilio/voice/{conversationId}/receive/0?states={states}";
        var response = twilio.ReturnInstructions("twilio/welcome.mp3", url, true);
        return TwiML(response);
    }

    [ValidateRequest]
    [HttpPost("twilio/voice/{conversationId}/receive/{seqNum}")]
    public async Task<TwiMLResult> ReceiveCallerMessage([FromRoute] string conversationId, [FromRoute] int seqNum, [FromQuery] string states, VoiceRequest request)
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var messageQueue = _services.GetRequiredService<TwilioMessageQueue>();
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var messages = await sessionManager.RetrieveStagedCallerMessagesAsync(conversationId, seqNum);
        if (!string.IsNullOrWhiteSpace(request.SpeechResult))
        {
            messages.Add(request.SpeechResult);
            await sessionManager.StageCallerMessageAsync(conversationId, seqNum, request.SpeechResult);
        }
        VoiceResponse response;
        if (messages.Count == 0 && seqNum == 0)
        { 
            response = twilio.ReturnInstructions("twilio/welcome.mp3", $"twilio/voice/{conversationId}/receive/{seqNum}?states={states}", true);
        }
        else
        {
            if (messages.Count == 0)
            {
                messages = await sessionManager.RetrieveStagedCallerMessagesAsync(conversationId, seqNum - 1);
            }
            var messageContent = string.Join("\r\n", messages);
            var callerMessage = new CallerMessage()
            {
                ConversationId = conversationId,
                SeqNumber = seqNum,
                Content = messageContent,
                From = request.From
            };
            if (!string.IsNullOrEmpty(states))
            {
                var kvp = states.Split(':');
                if (kvp.Length == 2)
                {
                    callerMessage.States.Add(kvp[0], kvp[1]);
                }
            }
            await messageQueue.EnqueueAsync(callerMessage);

            int audioIndex = Random.Shared.Next(1, 5);
            response = twilio.ReturnInstructions($"twilio/hold-on-{audioIndex}.mp3", $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true, 1);
        }
        return TwiML(response);
    }

    [ValidateRequest]
    [HttpPost("twilio/voice/{conversationId}/reply/{seqNum}")]
    public async Task<TwiMLResult> ReplyCallerMessage([FromRoute] string conversationId, [FromRoute] int seqNum, [FromQuery] string states, VoiceRequest request)
    {
        var nextSeqNum = seqNum + 1;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var twilio = _services.GetRequiredService<TwilioService>();
        if (request.SpeechResult != null)
        {
            await sessionManager.StageCallerMessageAsync(conversationId, nextSeqNum, request.SpeechResult);
        }
        var reply = await sessionManager.GetAssistantReplyAsync(conversationId, seqNum);
        VoiceResponse response;
        if (reply == null)
        {
            var indication = await sessionManager.GetReplyIndicationAsync(conversationId, seqNum);
            if (indication != null)
            {
                string speechPath;
                if (indication.StartsWith('#'))
                {
                    speechPath = $"twilio/{indication.Substring(1)}";
                }
                else
                {
                    var textToSpeechService = CompletionProvider.GetTextToSpeech(_services, "openai", "tts-1");
                    var fileService = _services.GetRequiredService<IFileStorageService>();
                    var data = await textToSpeechService.GenerateSpeechFromTextAsync(indication);
                    var fileName = $"indication_{seqNum}.mp3";
                    await fileService.SaveSpeechFileAsync(conversationId, fileName, data);
                    speechPath = $"twilio/voice/speeches/{conversationId}/{fileName}";
                }
                response = twilio.ReturnInstructions(speechPath, $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true, 2);
            }
            else
            {
                response = twilio.ReturnInstructions(null, $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true, 1);
            }
        }
        else
        {
            if (reply.ConversationEnd)
            {
                response = twilio.HangUp($"twilio/voice/speeches/{conversationId}/{reply.SpeechFileName}");
            }
            else
            {
                response = twilio.ReturnInstructions($"twilio/voice/speeches/{conversationId}/{reply.SpeechFileName}", $"twilio/voice/{conversationId}/receive/{nextSeqNum}?states={states}", true);
            }

        }
        return TwiML(response);
    }

    [ValidateRequest]
    [HttpGet("twilio/voice/speeches/{conversationId}/{fileName}")]
    public async Task<FileContentResult> RetrieveSpeechFile([FromRoute] string conversationId, [FromRoute] string fileName)
    {
        var fileService = _services.GetRequiredService<IFileStorageService>();
        var data = await fileService.RetrieveSpeechFileAsync(conversationId, fileName);
        var result = new FileContentResult(data.ToArray(), "audio/mpeg");
        result.FileDownloadName = fileName;
        return result;
    }
}
