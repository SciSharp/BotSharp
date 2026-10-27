using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Utilities;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Models.Relay;
using Twilio.Exceptions;
using Twilio.Jwt.AccessToken;
using CallResource = Twilio.Rest.Api.V2010.Account.CallResource;
using Twilio.Rest.Api.V2010.Account.Call;
using Task = System.Threading.Tasks.Task;
using Token = Twilio.Jwt.AccessToken.Token;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// https://github.com/TwilioDevEd/voice-javascript-sdk-quickstart-csharp
/// </summary>
public class TwilioService
{
    private const string TextSpeechExtension = ".txt";
    private const string AudioSpeechExtension = ".mp3";

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
            SpeechModel = _settings.SpeechModel,
            SpeechTimeout = "auto", // timeout > 0 ? timeout.ToString() : "3",
            Timeout = Math.Max(_settings.GatherTimeout, 1),
            ActionOnEmptyResult = conversationalVoiceResponse.ActionOnEmptyResult,
            Hints = conversationalVoiceResponse.Hints
        };

        if (!conversationalVoiceResponse.SpeechPaths.IsNullOrEmpty())
        {
            foreach (var speechPath in conversationalVoiceResponse.SpeechPaths)
            {
                AppendSpeech(gather, conversationalVoiceResponse.ConversationId, speechPath);
            }
        }

        if (!string.IsNullOrEmpty(conversationalVoiceResponse.Text))
        {
            gather.Append(CreateSay(conversationalVoiceResponse.Text));
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
                AppendSpeech(response, conversationId, speechPath);
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
            SpeechModel = _settings.SpeechModel,
            SpeechTimeout = "auto", // conversationalVoiceResponse.Timeout > 0 ? conversationalVoiceResponse.Timeout.ToString() : "3",
            Timeout = Math.Max(_settings.GatherTimeout, 1),
            ActionOnEmptyResult = voiceResponse.ActionOnEmptyResult,
        };
        response.Append(gather);

        return response;
    }

    public async Task StartRecording(string callSid, string agentId, string conversationId)
    {
        if (_settings.RecordingEnabled)
        {
            // https://help.twilio.com/articles/360010317333-Recording-Incoming-Twilio-Voice-Calls
            var recordStatusUrl = $"{_settings.CallbackHost}/twilio/record/status?agent-id={agentId}&conversation-id={conversationId}";
            var recording = await RecordingResource.CreateAsync(pathCallSid: callSid,
                recordingStatusCallback: new Uri(recordStatusUrl),
                trim: "trim-silence",
                recordingChannels: "dual",
                recordingTrack: "both");

            _logger.LogInformation($"Recording started: {recording.CallSid} {recording.Sid}");
        }
    }

    public VoiceResponse HangUp(string speechPath)
    {
        var response = new VoiceResponse();
        if (!string.IsNullOrEmpty(speechPath))
        {
            AppendSpeech(response, conversationId: null, speechPath);
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
                AppendSpeech(response, conversationId, speechPath);
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
            AppendSpeech(response, conversationId: null, speechPath);
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
                AppendSpeech(response, conversationId, speechPath);
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
            gather.Append(CreateSay(message));
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
            HookEmitter.Emit<IRealtimeHook>(_services, hook => words.AddRange(hook.OnModelTranscriptPrompt(agent)), 
                agent.Id);
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
                AppendSpeech(response, conversationId, speechPath);
            }
        }

        var connect = new Connect();
        var host = _settings.CallbackHost.Split("://").Last();
        connect.Stream(url: $"wss://{host}/twilio/stream/{agent.Id}/{conversationId}");
        response.Append(connect);

        return response;
    }

    /// <summary>
    /// Hands the call to Twilio ConversationRelay. Twilio opens a WebSocket to twilio/relay/{agentId}/{conversationId}
    /// and posts to twilio/relay/action when the session ends.
    /// </summary>
    public VoiceResponse ReturnConversationRelay(
        string agentId, string conversationId, string? welcomeGreeting, int reconnects = 0)
    {
        var relay = _settings.ConversationRelay;
        var query = $"agent-id={Uri.EscapeDataString(agentId)}" +
            $"&conversation-id={Uri.EscapeDataString(conversationId)}&reconnects={reconnects}";
        var connect = new Connect(action: new Uri($"{_settings.CallbackHost}/twilio/relay/action?{query}"));

        var element = new ConversationRelay(
            url: GetConversationRelayUrl(agentId, conversationId),
            language: relay.Language,
            ttsProvider: relay.TtsProvider,
            voice: relay.Voice,
            transcriptionProvider: relay.TranscriptionProvider,
            speechModel: relay.SpeechModel,
            dtmfDetection: relay.DtmfDetection,
            welcomeGreeting: string.IsNullOrWhiteSpace(welcomeGreeting) ? null : welcomeGreeting);

        // Attributes that the SDK's typed parameters don't cover (or only cover as booleans)
        element.SetOption("interruptible", relay.Interruptible);
        element.SetOption("welcomeGreetingInterruptible", relay.WelcomeGreetingInterruptible);
        element.SetOption("interruptSensitivity", relay.InterruptSensitivity);
        element.SetOption("ignoreBackchannel", relay.IgnoreBackchannel ? "true" : "false");
        if (!string.IsNullOrWhiteSpace(relay.Hints))
        {
            element.SetOption("hints", relay.Hints);
        }

        // Lets the session time its idle check from the end of the greeting
        var greetingLength = (welcomeGreeting?.Length ?? 0).ToString();
        element.Parameter(name: ConversationRelayParameters.WelcomeGreetingLength, value: greetingLength);

        connect.Append(element);
        var response = new VoiceResponse();
        response.Append(connect);
        return response;
    }

    public string GetConversationRelayUrl(string agentId, string conversationId)
    {
        // Each id is one path segment, so a "/" or "?" in it cannot change the route
        var host = _settings.CallbackHost.Split("://").Last();
        return $"wss://{host}/twilio/relay/{Uri.EscapeDataString(agentId)}/{Uri.EscapeDataString(conversationId)}";
    }

    public async Task<VoiceResponse> WaitingForAiResponse(ConversationalVoiceRequest request)
    {
        VoiceResponse response;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();

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
                    var fileName = await SaveSpeechAsync(request.ConversationId, $"indication_{hash}", seg,
                        reuseExisting: true);
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

            // No OnAuthenticate before these hooks — identity is established only at the HTTP entry points.
            await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook => await hook.OnWaitingAgentResponse(request, instruction),
                request.AgentId);

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

    /// <summary>
    /// Saves the speech of a text in the conversation and returns its file name: a text file spoken later with Say
    /// when SayGeneratedSpeech is on, otherwise an mp3 from the audio synthesizer.
    /// </summary>
    public async Task<string> SaveSpeechAsync(string conversationId, string name, string text, bool reuseExisting = false)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var fileName = name + (_settings.SayGeneratedSpeech ? TextSpeechExtension : AudioSpeechExtension);
        if (reuseExisting && !fileStorage.GetSpeechFile(conversationId, fileName).ToMemory().IsEmpty)
        {
            return fileName;
        }

        var data = _settings.SayGeneratedSpeech
            ? BinaryData.FromString(text)
            : await CompletionProvider.GetAudioSynthesizer(_services).GenerateAudioAsync(text);
        fileStorage.SaveSpeechFile(conversationId, fileName, data);
        return fileName;
    }

    /// <summary>
    /// Points a live call at new TwiML. Returns false when the call is no longer in progress (Twilio 21220): the caller
    /// already hung up, or the message did not come from a live call at all (e.g. a phone conversation continued over
    /// the REST API). Any other Twilio error is thrown.
    /// </summary>
    public async Task<bool> TryRedirectCallAsync(string callSid, string url)
    {
        try
        {
            await CallResource.UpdateAsync(pathSid: callSid, url: new Uri(url));
            return true;
        }
        catch (ApiException ex) when (ex.Code == CallNotInProgressErrorCode)
        {
            _logger.LogInformation($"Call {callSid} is no longer in progress, not redirected: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Twilio's "Call is not in-progress. Cannot redirect.",see https://www.twilio.com/docs/api/errors/21220
    /// </summary>
    private const int CallNotInProgressErrorCode = 21220;

    /// <summary>
    /// Returns the file name of a speech saved by SaveSpeechAsync, whichever mode it was saved in.
    /// </summary>
    public string GetSavedSpeechFile(string conversationId, string name)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var textFile = name + TextSpeechExtension;
        return fileStorage.GetSpeechFile(conversationId, textFile).ToMemory().IsEmpty
            ? name + AudioSpeechExtension
            : textFile;
    }

    /// <summary>
    /// Appends a speech to the TwiML: Say for a text file saved by SaveSpeechAsync, Play for an audio file.
    /// </summary>
    public void AppendSpeech(TwiML parent, string? conversationId, string speechPath)
    {
        if (_settings.SayGeneratedSpeech && TryGetPromptText(speechPath, out var promptText))
        {
            parent.Append(CreateSay(promptText));
            return;
        }

        if (!speechPath.EndsWith(TextSpeechExtension, StringComparison.OrdinalIgnoreCase))
        {
            parent.Append(new Play(new Uri(GetSpeechPath(conversationId, speechPath))));
            return;
        }

        var (convId, fileName) = SplitSpeechPath(conversationId, speechPath);
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var data = fileStorage.GetSpeechFile(convId, fileName);
        if (data.ToMemory().IsEmpty)
        {
            _logger.LogWarning($"Speech text {fileName} is missing (conversation {convId}).");
            return;
        }

        parent.Append(CreateSay(data.ToString()));
    }

    /// <summary>
    /// The configured text of a prerecorded prompt, or null when the path has none (e.g. a per-conversation file).
    /// ConversationRelay can only speak text, so this is how its greeting reuses the prompts a hook queued.
    /// </summary>
    public string? GetPromptText(string speechPath)
    {
        return TryGetPromptText(speechPath, out var text) ? text : null;
    }

    /// <summary>
    /// Looks up the configured text of a prerecorded prompt such as "twilio/say-it-again-1.mp3".
    /// Files generated per conversation (twilio/voice/speeches/...) never match.
    /// </summary>
    private bool TryGetPromptText(string speechPath, out string text)
    {
        text = string.Empty;
        const string promptRoute = "twilio/";
        var path = StripCallbackHost(speechPath);
        if (!path.StartsWith(promptRoute))
        {
            return false;
        }

        var fileName = path.Substring(promptRoute.Length);
        if (fileName.Contains('/'))
        {
            return false;
        }

        return _settings.PromptTexts.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(fileName), out text!)
            && !string.IsNullOrWhiteSpace(text);
    }

    private string StripCallbackHost(string speechPath)
    {
        return speechPath.StartsWith(_settings.CallbackHost)
            ? speechPath.Substring(_settings.CallbackHost.Length).TrimStart('/')
            : speechPath;
    }

    private (string? ConversationId, string FileName) SplitSpeechPath(string? conversationId, string speechPath)
    {
        var path = StripCallbackHost(speechPath);

        // Accepts "twilio/voice/speeches/{conversationId}/{fileName}" or a bare file name in the given conversation
        const string speechRoute = "twilio/voice/speeches/";
        if (path.StartsWith(speechRoute))
        {
            var parts = path.Substring(speechRoute.Length).Split('/');
            if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }
        }

        return (conversationId, path);
    }

    private Say CreateSay(string text)
    {
        return string.IsNullOrEmpty(_settings.SayVoice)
            ? new Say(text)
            : new Say(text, voice: _settings.SayVoice);
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
