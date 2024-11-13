using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Infrastructures;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
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
    public async Task<TwiMLResult> InitiateConversation(ConversationalVoiceRequest request)
    {
        var text = JsonSerializer.Serialize(request);
        if (request?.CallSid == null)
        {
            throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        }

        VoiceResponse response = null;
        request.ConversationId = $"TwilioVoice_{request.CallSid}";

        var instruction = new ConversationalVoiceResponse
        {
            SpeechPaths = ["twilio/welcome.mp3"],
            CallbackPath = $"twilio/voice/{request.ConversationId}/receive/0",
            ActionOnEmptyResult = true,
            Timeout = 2
        };
        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreating(request, instruction);
        }, new HookEmitOption
        {
            OnlyOnce = true
        });

        instruction.CallbackPath = $"{instruction.CallbackPath}?{GenerateStatesParameter(request.States)}";

        var twilio = _services.GetRequiredService<TwilioService>();
        if (string.IsNullOrWhiteSpace(request.Intent))
        {
            response = twilio.ReturnNoninterruptedInstructions(instruction);
        }
        else
        {
            int seqNum = 0;
            var messageQueue = _services.GetRequiredService<TwilioMessageQueue>();
            var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
            await sessionManager.StageCallerMessageAsync(request.ConversationId, seqNum, request.Intent);
            var callerMessage = new CallerMessage()
            {
                ConversationId = request.ConversationId,
                SeqNumber = seqNum,
                Content = request.Intent,
                From = request.From,
                States = ParseStates(request.States)
            };
            await messageQueue.EnqueueAsync(callerMessage);
            response = new VoiceResponse();
            response.Redirect(new Uri($"{_settings.CallbackHost}/twilio/voice/{request.ConversationId}/reply/{seqNum}?{GenerateStatesParameter(request.States)}"), HttpMethod.Post);
        }

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreated(request);
        }, new HookEmitOption
        {
            OnlyOnce = true
        });

        return TwiML(response);
    }

    /// <summary>
    /// Wait for caller's response
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [ValidateRequest]
    [HttpPost("twilio/voice/{conversationId}/receive/{seqNum}")]
    public async Task<TwiMLResult> ReceiveCallerMessage(ConversationalVoiceRequest request)
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var messageQueue = _services.GetRequiredService<TwilioMessageQueue>();
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();

        var messages = await sessionManager.RetrieveStagedCallerMessagesAsync(request.ConversationId, request.SeqNum);
        string text = (request.SpeechResult + "\r\n" + request.Digits).Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            messages.Add(text);
            await sessionManager.StageCallerMessageAsync(request.ConversationId, request.SeqNum, text);
        }

        VoiceResponse response = null;
        if (messages.Any())
        {
            var messageContent = string.Join("\r\n", messages);
            var callerMessage = new CallerMessage()
            {
                ConversationId = request.ConversationId,
                SeqNumber = request.SeqNum,
                Content = messageContent,
                Digits = request.Digits,
                From = request.From,
                States = ParseStates(request.States)
            };
            await messageQueue.EnqueueAsync(callerMessage);

            response = new VoiceResponse();
            response.Redirect(new Uri($"{_settings.CallbackHost}/twilio/voice/{request.ConversationId}/reply/{request.SeqNum}?{GenerateStatesParameter(request.States)}"), HttpMethod.Post);

            await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
            {
                await hook.OnReceivedUserMessage(request);
            }, new HookEmitOption
            {
                OnlyOnce = true
            });
        }
        else
        {
            // keep waiting for user response
            if (request.Attempts > 2)
            {
                var instruction = new ConversationalVoiceResponse
                {
                    SpeechPaths = new List<string>(),
                    CallbackPath = $"twilio/voice/{request.ConversationId}/receive/{request.SeqNum}?{GenerateStatesParameter(request.States)}",
                    ActionOnEmptyResult = true
                };

                // prompt user to speak clearly
                if (request.SeqNum == 0)
                {
                    instruction.SpeechPaths.Add("twilio/welcome.mp3");
                }
                else
                {
                    var lastRepy = await sessionManager.GetAssistantReplyAsync(request.ConversationId, request.SeqNum - 1);
                    instruction.SpeechPaths.Add($"twilio/voice/speeches/{request.ConversationId}/{lastRepy.SpeechFileName}");
                }

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnWaitingUserResponse(request, instruction);
                }, new HookEmitOption
                {
                    OnlyOnce = true
                });

                response = twilio.ReturnInstructions(instruction);
            }
            else
            {
                var instruction = new ConversationalVoiceResponse
                {
                    SpeechPaths = new List<string>(),
                    CallbackPath = $"twilio/voice/{request.ConversationId}/receive/{request.SeqNum}?{GenerateStatesParameter(request.States)}&attempts={++request.Attempts}",
                    ActionOnEmptyResult = true
                };

                if (request.Attempts == 2)
                {
                    instruction.SpeechPaths.Add($"twilio/say-it-again-{Random.Shared.Next(1, 5)}.mp3");
                }

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnWaitingUserResponse(request, instruction);
                }, new HookEmitOption
                {
                    OnlyOnce = true
                });

                response = twilio.ReturnInstructions(instruction);
            }
        }

        return TwiML(response);
    }

    /// <summary>
    /// Polling for assistant reply after user responsed
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [ValidateRequest]
    [HttpPost("twilio/voice/{conversationId}/reply/{seqNum}")]
    public async Task<TwiMLResult> ReplyCallerMessage(ConversationalVoiceRequest request)
    {
        var nextSeqNum = request.SeqNum + 1;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var twilio = _services.GetRequiredService<TwilioService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();

        if (request.SpeechResult != null)
        {
            await sessionManager.StageCallerMessageAsync(request.ConversationId, nextSeqNum, request.SpeechResult);
        }

        var reply = await sessionManager.GetAssistantReplyAsync(request.ConversationId, request.SeqNum);
        VoiceResponse response;

        if (reply == null)
        {
            var indication = await sessionManager.GetReplyIndicationAsync(request.ConversationId, request.SeqNum);
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

                        var fileName = $"indication_{request.SeqNum}_{segIndex}.mp3";
                        fileStorage.SaveSpeechFile(request.ConversationId, fileName, data);
                        speechPaths.Add($"twilio/voice/speeches/{request.ConversationId}/{fileName}");

                        // add typing
                        var typingIndex = Random.Shared.Next(1, 7);
                        if (typingIndex < 4)
                        {
                            speechPaths.Add($"twilio/typing-{typingIndex}.mp3");
                        }
                        segIndex++;
                    }
                }

                var instruction = new ConversationalVoiceResponse
                {
                    SpeechPaths = speechPaths,
                    CallbackPath = $"twilio/voice/{request.ConversationId}/reply/{request.SeqNum}?{GenerateStatesParameter(request.States)}",
                    ActionOnEmptyResult = true
                };

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnIndicationGenerated(request, instruction);
                }, new HookEmitOption
                {
                    OnlyOnce = true
                });

                response = twilio.ReturnInstructions(instruction);

                await sessionManager.RemoveReplyIndicationAsync(request.ConversationId, request.SeqNum);
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

                var instruction = new ConversationalVoiceResponse
                {
                    SpeechPaths = instructions,
                    CallbackPath = $"twilio/voice/{request.ConversationId}/reply/{request.SeqNum}?{GenerateStatesParameter(request.States)}",
                    ActionOnEmptyResult = true
                };

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnWaitingAgentResponse(request, instruction);
                }, new HookEmitOption
                {
                    OnlyOnce = true
                });

                response = twilio.ReturnInstructions(instruction);
            }
        }
        else
        {
            if (reply.HumanIntervationNeeded)
            {
                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnAgentTransferring(request, _settings);
                }, new HookEmitOption
                {
                    OnlyOnce = true
                });

                response = twilio.DialCsrAgent($"twilio/voice/speeches/{request.ConversationId}/{reply.SpeechFileName}");
            }
            else if (reply.ConversationEnd)
            {
                response = twilio.HangUp($"twilio/voice/speeches/{request.ConversationId}/{reply.SpeechFileName}");

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnAgentHangUp(request);
                }, new HookEmitOption
                {
                    OnlyOnce = true
                });
            }
            else
            {
                var instruction = new ConversationalVoiceResponse
                {
                    SpeechPaths = [$"twilio/voice/speeches/{request.ConversationId}/{reply.SpeechFileName}"],
                    CallbackPath = $"twilio/voice/{request.ConversationId}/receive/{nextSeqNum}?{GenerateStatesParameter(request.States)}",
                    ActionOnEmptyResult = true,
                    Hints = reply.Hints
                };

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnAgentResponsing(request, instruction);
                }, new HookEmitOption
                {
                    OnlyOnce = true
                });

                response = twilio.ReturnInstructions(instruction);
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

    private Dictionary<string, string> ParseStates(List<string> states)
    {
        var result = new Dictionary<string, string>();
        if (states is null || !states.Any())
        {
            return result;
        }
        foreach (var kvp in states)
        {
            var parts = kvp.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                result.Add(parts[0], parts[1]);
            }
        }
        return result;
    }

    private string GenerateStatesParameter(List<string> states)
    {
        if (states is null || states.Count == 0)
        {
            return null;
        }
        return string.Join("&", states.Select(x => $"states={x}"));
    }
}
