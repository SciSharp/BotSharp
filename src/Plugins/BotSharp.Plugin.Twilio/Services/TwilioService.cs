using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Utilities;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using Twilio.Jwt.AccessToken;
using Token = Twilio.Jwt.AccessToken.Token;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// https://github.com/TwilioDevEd/voice-javascript-sdk-quickstart-csharp
/// </summary>
public class TwilioService
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    public readonly ILogger _logger;

    public TwilioService(TwilioSetting settings, IServiceProvider services, ILogger<TwilioService> logger)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }

    public string GetAccessToken()
    {
        // These are specific to Voice
        var user = _services.GetRequiredService<IUserIdentity>();

        // Create a Voice grant for this token
        var grant = new VoiceGrant();
        grant.OutgoingApplicationSid = _settings.AppSID;

        // Optional: add to allow incoming calls
        grant.IncomingAllow = true;

        var grants = new HashSet<IGrant>
        {
            { grant }
        };

        // Create an Access Token generator
        var token = new Token(
            _settings.AccountSID,
            _settings.ApiKeySID,
            _settings.ApiSecret,
            user.Id,
            grants: grants);

        return token.ToJwt();
    }

    public VoiceResponse ReturnInstructions(ConversationalVoiceResponse conversationalVoiceResponse)
    {
        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/{conversationalVoiceResponse.CallbackPath}"),
            Enhanced = true,
            SpeechModel = Gather.SpeechModelEnum.PhoneCall,
            SpeechTimeout = "auto", // timeout > 0 ? timeout.ToString() : "3",
            Timeout = conversationalVoiceResponse.Timeout > 0 ? conversationalVoiceResponse.Timeout : 3,
            ActionOnEmptyResult = conversationalVoiceResponse.ActionOnEmptyResult,
            Hints = conversationalVoiceResponse.Hints
        };

        if (!conversationalVoiceResponse.SpeechPaths.IsNullOrEmpty())
        {
            foreach (var speechPath in conversationalVoiceResponse.SpeechPaths)
            {
                gather.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
            }
        }
        response.Append(gather);
        return response;
    }

    public VoiceResponse ReturnNoninterruptedInstructions(ConversationalVoiceResponse voiceResponse)
    {
        var response = new VoiceResponse();
        var conversationId = voiceResponse.ConversationId;
        if (voiceResponse.SpeechPaths != null && voiceResponse.SpeechPaths.Any())
        {
            foreach (var speechPath in voiceResponse.SpeechPaths)
            {
                var uri = GetSpeechPath(conversationId, speechPath);
                response.Play(new Uri(uri));
            }
        }

        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/{voiceResponse.CallbackPath}"),
            Enhanced = true,
            SpeechModel = Gather.SpeechModelEnum.PhoneCall,
            SpeechTimeout = "auto", // conversationalVoiceResponse.Timeout > 0 ? conversationalVoiceResponse.Timeout.ToString() : "3",
            Timeout = voiceResponse.Timeout > 0 ? voiceResponse.Timeout : 3,
            ActionOnEmptyResult = voiceResponse.ActionOnEmptyResult,
        };
        response.Append(gather);

        return response;
    }

    public VoiceResponse HangUp(string speechPath)
    {
        var response = new VoiceResponse();
        if (!string.IsNullOrEmpty(speechPath))
        {
            response.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
        }
        response.Hangup();
        return response;
    }

    public VoiceResponse HangUp(ConversationalVoiceResponse voiceResponse)
    {
        var response = new VoiceResponse();
        var conversationId = voiceResponse.ConversationId;
        if (voiceResponse.SpeechPaths != null && voiceResponse.SpeechPaths.Any())
        {
            foreach (var speechPath in voiceResponse.SpeechPaths)
            {
                var uri = GetSpeechPath(conversationId, speechPath);
                response.Play(new Uri(uri));
            }
        }
        response.Hangup();
        return response;
    }

    public VoiceResponse DialCsrAgent(string speechPath)
    {
        var response = new VoiceResponse();
        if (!string.IsNullOrEmpty(speechPath))
        {
            response.Play(new Uri($"{_settings.CallbackHost}/{speechPath}"));
        }
        response.Dial(_settings.CsrAgentNumber);
        return response;
    }

    public VoiceResponse TransferCall(ConversationalVoiceResponse conversationalVoiceResponse)
    {
        var response = new VoiceResponse();
        var conversationId = conversationalVoiceResponse.ConversationId;
        if (conversationalVoiceResponse.SpeechPaths != null && conversationalVoiceResponse.SpeechPaths.Any())
        {
            foreach (var speechPath in conversationalVoiceResponse.SpeechPaths)
            {
                var uri = GetSpeechPath(conversationId, speechPath);
                response.Play(new Uri(uri));
            }
        }
        response.Dial(conversationalVoiceResponse.TransferTo, answerOnBridge: true);

        return response;
    }

    public VoiceResponse HoldOn(int interval, string message = null)
    {
        var twilioSetting = _services.GetRequiredService<TwilioSetting>();

        var response = new VoiceResponse();
        var gather = new Gather()
        {
            Input = new List<Gather.InputEnum>()
            {
                Gather.InputEnum.Speech,
                Gather.InputEnum.Dtmf
            },
            Action = new Uri($"{_settings.CallbackHost}/twilio/voice/"),
            ActionOnEmptyResult = true
        };

        if (!string.IsNullOrEmpty(message))
        {
            gather.Say(message);
        }
        gather.Pause(interval);
        response.Append(gather);
        return response;
    }

    /// <summary>
    /// Bidirectional Media Streams
    /// </summary>
    /// <param name="conversationalVoiceResponse"></param>
    /// <returns></returns>
    public VoiceResponse ReturnBidirectionalMediaStreamsInstructions(ConversationalVoiceResponse conversationalVoiceResponse, Agent agent)
    {
        var response = new VoiceResponse();

        var conversationId = conversationalVoiceResponse.ConversationId;

        if (_settings.TranscribeEnabled)
        {
            var words = new List<string>();
            HookEmitter.Emit<IRealtimeHook>(_services, hook => words.AddRange(hook.OnModelTranscriptPrompt(agent)));
            var hints = string.Join(", ", words);
            var start = new Start();
            start.Transcription(
                track: "inbound_track",
                partialResults: false,
                statusCallbackUrl: $"{_settings.CallbackHost}/twilio/transcribe?agent-id={conversationalVoiceResponse.AgentId}&conversation-id={conversationId}", 
                name: conversationId,
                hints: hints);
            response.Append(start);
        }

        if (conversationalVoiceResponse.SpeechPaths != null && conversationalVoiceResponse.SpeechPaths.Any())
        {
            foreach (var speechPath in conversationalVoiceResponse.SpeechPaths)
            {
                var uri = GetSpeechPath(conversationId, speechPath);
                response.Play(new Uri(uri));
            }
        }

        var connect = new Connect();
        var host = _settings.CallbackHost.Split("://").Last();
        connect.Stream(url: $"wss://{host}/twilio/stream/{conversationId}");
        response.Append(connect);

        return response;
    }

    public async Task<VoiceResponse> WaitingForAiResponse(ConversationalVoiceRequest request)
    {
        VoiceResponse response;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();

        var indication = await sessionManager.GetReplyIndicationAsync(request.ConversationId, request.SeqNum);
        if (indication != null)
        {
            _logger.LogWarning($"Indication ({request.SeqNum}): {indication}");
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
                    var hash = Utilities.HashTextMd5(seg);
                    var fileName = $"indication_{hash}.mp3";

                    var existing = fileStorage.GetSpeechFile(request.ConversationId, fileName);
                    if (existing == BinaryData.Empty)
                    {
                        var completion = CompletionProvider.GetAudioSynthesizer(_services);
                        var data = await completion.GenerateAudioAsync(seg);
                        fileStorage.SaveSpeechFile(request.ConversationId, fileName, data);
                    }

                    speechPaths.Add($"twilio/voice/speeches/{request.ConversationId}/{fileName}");
                }
            }

            var instruction = new ConversationalVoiceResponse
            {
                AgentId = request.AgentId,
                ConversationId = request.ConversationId,
                SpeechPaths = speechPaths,
                CallbackPath = $"twilio/voice/reply/{request.SeqNum}?agent-id={request.AgentId}&conversation-id={request.ConversationId}&{GenerateStatesParameter(request.States)}&AIResponseWaitTime={++request.AIResponseWaitTime}",
                ActionOnEmptyResult = true
            };

            response = ReturnInstructions(instruction);

            await sessionManager.RemoveReplyIndicationAsync(request.ConversationId, request.SeqNum);
        }
        else
        {
            var instruction = new ConversationalVoiceResponse
            {
                AgentId = request.AgentId,
                ConversationId = request.ConversationId,
                SpeechPaths = [],
                CallbackPath = $"twilio/voice/reply/{request.SeqNum}?agent-id={request.AgentId}&conversation-id={request.ConversationId}&{GenerateStatesParameter(request.States)}&AIResponseWaitTime={++request.AIResponseWaitTime}",
                ActionOnEmptyResult = true
            };

            await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
            {
                await hook.OnWaitingAgentResponse(request, instruction);
            });

            response = ReturnInstructions(instruction);
        }

        return response;
    }

    /// <summary>
    /// https://www.twilio.com/docs/voice/answering-machine-detection
    /// </summary>
    /// <param name="answeredBy"></param>
    /// <returns></returns>
    public bool MachineDetected(ConversationalVoiceRequest request)
    {
        var answeredBy = request.AnsweredBy ?? "unknown";
        var isOutboundCall = request.Direction == "outbound-api";
        var isMachine = answeredBy.StartsWith("machine_") || answeredBy == "fax";
        return isOutboundCall && isMachine;
    }

    public string GetSpeechPath(string conversationId, string speechPath)
    {
        if (speechPath.StartsWith("twilio/"))
        {
            return $"{_settings.CallbackHost}/{speechPath}";
        }
        else if (speechPath.StartsWith(_settings.CallbackHost))
        {
            return speechPath;
        }
        else
        {
            return $"{_settings.CallbackHost}/twilio/voice/speeches/{conversationId}/{speechPath}";
        }
    }

    public string GenerateStatesParameter(List<string> states)
    {
        if (states is null || states.Count == 0)
        {
            return null;
        }
        return string.Join("&", states.Select(x => $"states={x}"));
    }
}
