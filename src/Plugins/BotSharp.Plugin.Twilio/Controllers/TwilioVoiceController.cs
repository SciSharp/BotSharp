using BotSharp.Abstraction.Files;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Twilio.Http;

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
        var response = twilio.ReturnInstructions(new List<string> { "twilio/welcome.mp3" }, url, true, timeout: 1);
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
        string text = (request.SpeechResult + "\r\n" + request.Digits).Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            messages.Add(text);
            await sessionManager.StageCallerMessageAsync(conversationId, seqNum, text);
        }

        VoiceResponse response;
        if (messages.Any())
        {
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

            response = new VoiceResponse()
                .Redirect(new Uri($"{_settings.CallbackHost}/twilio/voice/{conversationId}/reply/{seqNum}?states={states}"), HttpMethod.Post);
        }
        else
        {
            response = twilio.ReturnInstructions(null, $"twilio/voice/{conversationId}/receive/{seqNum}?states={states}", true);
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
                var speechPaths = new List<string>();
                foreach (var text in indication.Split('|'))
                {
                    var seg = text.Trim();
                    if (seg.StartsWith('#'))
                    {
                        speechPaths.Add($"twilio/{seg.Substring(1)}.mp3");
                    }
                    else
                    {
                        var textToSpeechService = CompletionProvider.GetTextToSpeech(_services, "openai", "tts-1");
                        var fileService = _services.GetRequiredService<IFileStorageService>();
                        var data = await textToSpeechService.GenerateSpeechFromTextAsync(seg);
                        var fileName = $"indication_{seqNum}.mp3";
                        await fileService.SaveSpeechFileAsync(conversationId, fileName, data);
                        speechPaths.Add($"twilio/voice/speeches/{conversationId}/{fileName}");
                    }
                }
                response = twilio.ReturnInstructions(speechPaths, $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true);
            }
            else
            {
                int audioIndex = Random.Shared.Next(1, 4);
                response = twilio.ReturnInstructions(new List<string> { $"{_settings.CallbackHost}/twilio/typing-{audioIndex}.mp3" }, $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true, 1);
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
                response = twilio.ReturnInstructions(new List<string> { $"twilio/voice/speeches/{conversationId}/{reply.SpeechFileName}" }, $"twilio/voice/{conversationId}/receive/{nextSeqNum}?states={states}", true);
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
