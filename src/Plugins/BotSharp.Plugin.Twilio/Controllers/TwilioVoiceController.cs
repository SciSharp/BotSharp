using BotSharp.Abstraction.Files;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Twilio.Http;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioVoiceController : TwilioController
{
    protected readonly TwilioSetting _settings;
    protected readonly IServiceProvider _services;
    protected readonly IHttpContextAccessor _context;
    protected readonly ILogger _logger;

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
    [Obsolete("Use twilio/inbound for streaming and non-streaming instead of this endpoint")]
    [ValidateRequest]
    [HttpPost("twilio/voice/welcome")]
    public async Task<TwiMLResult> InitiateConversation(ConversationalVoiceRequest request)
    {
        foreach(var header in Request.Headers)
        {
            _logger.LogWarning($"{header.Key}: {header.Value}");
        }

        _logger.LogWarning($"{Request.Path}{Request.QueryString}");

        var text = JsonSerializer.Serialize(request);
        if (request?.CallSid == null)
        {
            throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        }

        VoiceResponse response = default!;
        request.ConversationId = $"twilio_{request.CallSid}";

        var instruction = new ConversationalVoiceResponse
        {
            AgentId = request.AgentId,
            ConversationId = request.ConversationId,
            SpeechPaths = [$"twilio/welcome-{request.AgentId}.mp3"],
            ActionOnEmptyResult = true
        };

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreating(request, instruction);
        });

        var twilio = _services.GetRequiredService<TwilioService>();
        if (string.IsNullOrWhiteSpace(request.Intent))
        {
            instruction.CallbackPath = $"twilio/voice/receive/0?agent-id={request.AgentId}&conversation-id={request.ConversationId}&{twilio.GenerateStatesParameter(request.States)}";
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
                AgentId = request.AgentId,
                ConversationId = request.ConversationId,
                SeqNumber = seqNum,
                Content = request.Intent,
                From = request.From,
                States = ParseStates(request.States)
            };
            await messageQueue.EnqueueAsync(callerMessage);
            response = new VoiceResponse();
            // delay 3 seconds to wait for the first message reply and caller is listening dudu sound
            await Task.Delay(1000 * 3);
            response.Redirect(new Uri($"{_settings.CallbackHost}/twilio/voice/reply/{seqNum}?agent-id={request.AgentId}&conversation-id={request.ConversationId}&{twilio.GenerateStatesParameter(request.States)}"), HttpMethod.Post);
        }

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreated(request);
        });

        return TwiML(response);
    }

    /// <summary>
    /// Wait for caller's response
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [ValidateRequest]
    [HttpPost("twilio/voice/receive/{seqNum}")]
    public async Task<TwiMLResult> ReceiveCallerMessage(ConversationalVoiceRequest request)
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var messageQueue = _services.GetRequiredService<TwilioMessageQueue>();
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();

        // Fetch all accumulated caller message.
        var messages = await sessionManager.RetrieveStagedCallerMessagesAsync(request.ConversationId, request.SeqNum);
        string text = (request.SpeechResult + "\r\n" + request.Digits).Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            // Concanate with incoming message
            messages.Add(text);
            await sessionManager.StageCallerMessageAsync(request.ConversationId, request.SeqNum, text);
        }

        VoiceResponse response = default!;
        if (messages.Any())
        {
            var messageContent = string.Join("\r\n", messages);
            var callerMessage = new CallerMessage()
            {
                AgentId = request.AgentId,
                ConversationId = request.ConversationId,
                SeqNumber = request.SeqNum,
                Content = messageContent,
                Digits = request.Digits,
                From = string.Equals(request.Direction, "inbound") ? request.From : request.To,
                States = ParseStates(request.States)
            };
            callerMessage.RequestHeaders = new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[Request.Headers.Count];
            Request.Headers.CopyTo(callerMessage.RequestHeaders, 0);
            await messageQueue.EnqueueAsync(callerMessage);

            response = new VoiceResponse();
            response.Redirect(new Uri($"{_settings.CallbackHost}/twilio/voice/reply/{request.SeqNum}?agent-id={request.AgentId}&conversation-id={request.ConversationId}&{twilio.GenerateStatesParameter(request.States)}&AIResponseWaitTime=0"), HttpMethod.Post);

            await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
            {
                await hook.OnReceivedUserMessage(request);
            });
        }
        else
        {
            if (request.Attempts > _settings.MaxGatherAttempts)
            {

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnAgentHangUp(request);
                });

                response = twilio.HangUp(string.Empty);
            }
            // keep waiting for user response
            else
            {
                var instruction = new ConversationalVoiceResponse
                {
                    AgentId = request.AgentId,
                    ConversationId = request.ConversationId,
                    SpeechPaths = new List<string>(),
                    CallbackPath = $"twilio/voice/receive/{request.SeqNum}?agent-id={request.AgentId}&conversation-id={request.ConversationId}&{twilio.GenerateStatesParameter(request.States)}&attempts={++request.Attempts}",
                    ActionOnEmptyResult = true
                };

                if (request.Attempts == 3)
                {
                    instruction.SpeechPaths.Add($"twilio/say-it-again-{Random.Shared.Next(1, 5)}.mp3");
                }

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnWaitingUserResponse(request, instruction);
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
    [HttpPost("twilio/voice/reply/{seqNum}")]
    public async Task<TwiMLResult> ReplyCallerMessage(ConversationalVoiceRequest request)
    {
        var nextSeqNum = request.SeqNum + 1;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var twilio = _services.GetRequiredService<TwilioService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var text = (request.SpeechResult + "\r\n" + request.Digits).Trim();
        if (!string.IsNullOrEmpty(text))
        {
            await sessionManager.StageCallerMessageAsync(request.ConversationId, nextSeqNum, text);
        }

        var reply = await sessionManager.GetAssistantReplyAsync(request.ConversationId, request.SeqNum);
        VoiceResponse response;
        
        if (request.AIResponseWaitTime > 5)
        {
            // Wait AI Response Timeout
            await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
            {
                request.AIResponseErrorMessage = $"AI response timeout: AIResponseWaitTime greater than {request.AIResponseWaitTime}, please check internal error log!";
                await hook.OnAgentHangUp(request);
            });

            response = twilio.HangUp($"twilio/error.mp3");
        }
        else if (reply == null)
        {
            response = await twilio.WaitingForAiResponse(request);
        }
        else
        {
            if (reply.HumanIntervationNeeded)
            {
                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnAgentTransferring(request, _settings);
                });

                response = twilio.DialCsrAgent($"twilio/voice/speeches/{request.ConversationId}/{reply.SpeechFileName}");
            }
            else if (reply.ConversationEnd)
            {
                response = twilio.HangUp($"twilio/voice/speeches/{request.ConversationId}/{reply.SpeechFileName}");

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnAgentHangUp(request);
                });
            }
            else
            {
                var instruction = new ConversationalVoiceResponse
                {
                    AgentId = request.AgentId,
                    ConversationId = request.ConversationId,
                    SpeechPaths = [$"twilio/voice/speeches/{request.ConversationId}/{reply.SpeechFileName}"],
                    CallbackPath = $"twilio/voice/receive/{nextSeqNum}?agent-id={request.AgentId}&conversation-id={request.ConversationId}&{twilio.GenerateStatesParameter(request.States)}",
                    ActionOnEmptyResult = true,
                    Hints = reply.Hints
                };

                await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
                {
                    await hook.OnAgentResponsing(request, instruction);
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

    [ValidateRequest]
    [HttpPost("twilio/voice/hang-up")]
    public async Task<TwiMLResult> Hangup(ConversationalVoiceRequest request)
    {
        var instruction = new ConversationalVoiceResponse
        {
            AgentId = request.AgentId,
            ConversationId = request.ConversationId
        };

        if (request.InitAudioFile != null)
        {
            instruction.SpeechPaths.Add(request.InitAudioFile);
        }

        var twilio = _services.GetRequiredService<TwilioService>();
        var response = twilio.HangUp(instruction);
        return TwiML(response);
    }

    [ValidateRequest]
    [HttpPost("twilio/voice/transfer-call")]
    public async Task<TwiMLResult> TransferCall(ConversationalVoiceRequest request)
    {
        var instruction = new ConversationalVoiceResponse
        {
            AgentId = request.AgentId,
            ConversationId = request.ConversationId,
            TransferTo = request.TransferTo
        };

        if (request.InitAudioFile != null)
        {
            instruction.SpeechPaths.Add(request.InitAudioFile);
        }

        var twilio = _services.GetRequiredService<TwilioService>();
        var response = twilio.TransferCall(instruction);
        return TwiML(response);
    }

    [ValidateRequest]
    [HttpPost("twilio/voice/status")]
    public async Task<ActionResult> PhoneCallStatus(ConversationalVoiceRequest request)
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        if (request.CallStatus == "completed")
        {
            if (twilio.MachineDetected(request))
            {
                // voicemail
                await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                    async hook => await hook.OnVoicemailLeft(request));
            }
            else
            {
                // phone call completed
                await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                    async x => await x.OnUserDisconnected(request));
            }
        }
        else if (request.CallStatus == "busy")
        {
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                async x => await x.OnCallBusyStatus(request));
        }
        else if (request.CallStatus == "no-answer")
        {
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                async x => await x.OnCallNoAnswerStatus(request));
        }
        else if (request.CallStatus == "canceled")
        {
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                async x => await x.OnCallCanceledStatus(request));
        }
        else if (request.CallStatus == "failed")
        {
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                async x => await x.OnCallFailedStatus(request));
        }

        return Ok();
    }

    protected Dictionary<string, string> ParseStates(List<string> states)
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
}
