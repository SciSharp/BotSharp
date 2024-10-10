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
    private readonly ILogger _logger;

    public TwilioVoiceController(TwilioSetting settings, IServiceProvider services, IHttpContextAccessor context, ILogger<TwilioVoiceController> logger)
    {
        _settings = settings;
        _services = services;
        _context = context;
        _logger = logger;
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
        if (request?.CallSid == null)
        {
            throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        }

        string conversationId = $"TwilioVoice_{request.CallSid}";
        var twilio = _services.GetRequiredService<TwilioService>();
        var url = $"twilio/voice/{conversationId}/receive/0?states={states}";
        var response = twilio.ReturnNoninterruptedInstructions(new List<string> { "twilio/welcome.mp3" }, url, true, timeout: 2);
        return TwiML(response);
    }

    [ValidateRequest]
    [HttpPost("twilio/voice/{conversationId}/receive/{seqNum}")]
    public async Task<TwiMLResult> ReceiveCallerMessage([FromRoute] string conversationId, [FromRoute] int seqNum, [FromQuery] string states, VoiceRequest request, [FromQuery] int attempts = 1)
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

            response = new VoiceResponse().Redirect(new Uri($"{_settings.CallbackHost}/twilio/voice/{conversationId}/reply/{seqNum}?states={states}"), HttpMethod.Post);
        }
        else
        {
            if (attempts >= 2)
            {
                var speechPaths = new List<string>();

                if (seqNum == 0)
                {
                    speechPaths.Add("twilio/welcome.mp3");
                }
                else
                {
                    var lastRepy = await sessionManager.GetAssistantReplyAsync(conversationId, seqNum - 1);
                    speechPaths.Add($"twilio/say-it-again-{Random.Shared.Next(1, 5)}.mp3");
                    speechPaths.Add($"twilio/voice/speeches/{conversationId}/{lastRepy.SpeechFileName}");
                }
                response = twilio.ReturnInstructions(speechPaths, $"twilio/voice/{conversationId}/receive/{seqNum}?states={states}", true);
            }
            else
            {
                response = twilio.ReturnInstructions(null, $"twilio/voice/{conversationId}/receive/{seqNum}?states={states}&attempts={++attempts}", true);
            }
        }
        return TwiML(response);
    }

    [ValidateRequest]
    [HttpPost("twilio/voice/{conversationId}/reply/{seqNum}")]
    public async Task<TwiMLResult> ReplyCallerMessage([FromRoute] string conversationId, [FromRoute] int seqNum,
        [FromQuery] string states, VoiceRequest request)
    {
        var nextSeqNum = seqNum + 1;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var twilio = _services.GetRequiredService<TwilioService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();

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
                _logger.LogWarning($"Indication: {indication}");
                var speechPaths = new List<string>();
                int segIndex = 0;
                foreach (var text in indication.Split('|'))
                {
                    var seg = text.Trim();
                    if (seg.StartsWith('#'))
                    {
                        speechPaths.Add($"twilio/{seg.Substring(1)}.mp3");
                    }
                    else
                    {
                        var completion = CompletionProvider.GetAudioCompletion(_services, "openai", "tts-1");
                        var data = await completion.GenerateAudioFromTextAsync(seg);

                        // add hold-on
                        var holdOnIndex = Random.Shared.Next(1, 10);
                        if (holdOnIndex < 7)
                        {
                            speechPaths.Add($"twilio/hold-on-short-{holdOnIndex}.mp3");
                        }
                        
                        var fileName = $"indication_{seqNum}_{segIndex}.mp3";
                        fileStorage.SaveSpeechFile(conversationId, fileName, data);
                        speechPaths.Add($"twilio/voice/speeches/{conversationId}/{fileName}");

                        // add typing
                        var typingIndex = Random.Shared.Next(1, 7);
                        if (typingIndex < 4)
                        {
                            speechPaths.Add($"twilio/typing-{typingIndex}.mp3");
                        }
                        segIndex++;
                    }
                }
                response = twilio.ReturnInstructions(speechPaths, $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true);
                await sessionManager.RemoveReplyIndicationAsync(conversationId, seqNum);
            }
            else
            {
                var instructions = new List<string>
                {
                };

                // add hold-on
                var holdOnIndex = Random.Shared.Next(1, 15);
                if (holdOnIndex < 9)
                {
                    instructions.Add($"twilio/hold-on-long-{holdOnIndex}.mp3");
                }

                // add typing
                var typingIndex = Random.Shared.Next(1, 7);
                if (typingIndex < 4)
                {
                    instructions.Add($"twilio/typing-{typingIndex}.mp3");
                }

                response = twilio.ReturnInstructions(instructions, $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true);
            }
        }
        else
        {
            if (reply.HumanIntervationNeeded)
            {
                response = twilio.DialCsrAgent($"twilio/voice/speeches/{conversationId}/{reply.SpeechFileName}");
            }
            else if (reply.ConversationEnd)
            {
                response = twilio.HangUp($"twilio/voice/speeches/{conversationId}/{reply.SpeechFileName}");
            }
            else
            {
                response = twilio.ReturnInstructions(new List<string>
                {
                    $"twilio/voice/speeches/{conversationId}/{reply.SpeechFileName}"
                }, $"twilio/voice/{conversationId}/receive/{nextSeqNum}?states={states}", true, hints:reply.Hints);
            }
        }

        return TwiML(response);
    }

    [ValidateRequest]
    [HttpGet("twilio/voice/speeches/{conversationId}/{fileName}")]
    public async Task<FileContentResult> GetSpeechFile([FromRoute] string conversationId, [FromRoute] string fileName)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var data = fileStorage.GetSpeechFile(conversationId, fileName);
        var result = new FileContentResult(data.ToArray(), "audio/mpeg")
        {
            FileDownloadName = fileName
        };
        return result;
    }
}
