using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Twilio.Http;
using Conversation = BotSharp.Abstraction.Conversations.Models.Conversation;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Controllers;

public class TwilioInboundController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger _logger;

    public TwilioInboundController(TwilioSetting settings, IServiceProvider services, IHttpContextAccessor context, ILogger<TwilioOutboundController> logger)
    {
        _settings = settings;
        _services = services;
        _context = context;
        _logger = logger;
    }

    [ValidateRequest]
    [HttpPost("twilio/inbound")]
    public async Task<TwiMLResult> InitiateStreamConversation(ConversationalVoiceRequest request)
    {
        if (request?.CallSid == null)
        {
            throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        }

        var twilio = _services.GetRequiredService<TwilioService>();
        VoiceResponse response = default!;

        var instruction = new ConversationalVoiceResponse
        {
            AgentId = request.AgentId,
            ConversationId = request.ConversationId,
            SpeechPaths = [],
            ActionOnEmptyResult = true,
        };

        if (request.InitAudioFile != null)
        {
            instruction.SpeechPaths.Add(request.InitAudioFile);
        }

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreating(request, instruction);
        }, request.AgentId);

        var (agent, conversationId) = await InitConversation(request);
        request.ConversationId = conversationId.Id;
        instruction.AgentId = request.AgentId;
        instruction.ConversationId = request.ConversationId;

        if (twilio.MachineDetected(request))
        {
            response = new VoiceResponse();
            
            await HookEmitter.Emit<ITwilioCallStatusHook>(_services, 
                async hook => await hook.OnVoicemailStarting(request), request.AgentId);

            var url = twilio.GetSpeechPath(request.ConversationId, "voicemail.mp3");
            response.Play(new Uri(url));
        }
        else
        {
            if (agent.Labels.Contains("realtime"))
            {
                response = twilio.ReturnBidirectionalMediaStreamsInstructions(instruction, agent);
            }
            else
            {
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
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(1500);
                await twilio.StartRecording(request.CallSid, request.AgentId, request.ConversationId);
            });
        }

        await HookEmitter.Emit<ITwilioSessionHook>(_services, async hook =>
        {
            await hook.OnSessionCreated(request);
        }, request.AgentId);

        return TwiML(response);
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

    private async Task<(Agent, Conversation)> InitConversation(ConversationalVoiceRequest request)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conversation = await convService.GetConversation(request.ConversationId);
        if (conversation == null)
        {
            var conv = new Conversation
            {
                AgentId = request.AgentId,
                Channel = ConversationChannel.Phone,
                ChannelId = request.CallSid,
                Title = request.Intent ?? $"Incoming phone call from {request.From}",
                Tags = [],
            };

            conversation = await convService.NewConversation(conv);
        }

        var states = new List<MessageState>
        {
            new("channel", ConversationChannel.Phone),
            new("calling_phone", request.From),
            new("phone_direction", request.Direction),
            new("twilio_call_sid", request.CallSid),
        };

        if (request.Direction == "inbound")
        {
            states.Add(new MessageState("calling_phone_from", request.From));
            states.Add(new MessageState("calling_phone_to", request.To));
        }

        var requestStates = ParseStates(request.States);
        foreach (var s in requestStates)
        {
            if (!states.Any(x => x.Key == s.Key))
            {
                states.Add(new MessageState(s.Key, s.Value));
            }
        }

        if (request.InitAudioFile != null)
        {
            states.Add(new("init_audio_file", request.InitAudioFile));
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        // Get agent from storage
        var agent = await agentService.GetAgent(request.AgentId);
        if (agent.Type == AgentType.Routing)
        {
            states.Add(new(StateConst.ROUTING_MODE, agent.Mode));
        }

        convService.SetConversationId(conversation.Id, states);

        if (!string.IsNullOrEmpty(request.Intent))
        {
            var storage = _services.GetRequiredService<IConversationStorage>();

            storage.Append(conversation.Id, new RoleDialogModel(AgentRole.User, request.Intent)
            {
                CurrentAgentId = conversation.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        convService.SaveStates();
        
        // reload agent rendering with states
        agent = await agentService.LoadAgent(request.AgentId);

        return (agent, conversation);
    }
}
