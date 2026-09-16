using BotSharp.Abstraction.Routing;

namespace BotSharp.Plugin.OpenAI.Providers.Live;

/// <summary>
/// Client delegation: the voice model hands the thinking to this application instead of to a
/// managed Responses backend, and BotSharp's own routing pipeline answers it.
///
/// The awkward part of the protocol is that session.delegation.created carries only metadata -
/// an id and an offset - never the task text. The utterance has to come from the transcript
/// stream, and the two arrive in either order: the model usually delegates as soon as it grasps
/// the intent, which is before the user has stopped talking and therefore before the transcript
/// settles. So a delegation and an utterance are paired up here, whichever lands first.
/// </summary>
public partial class LiveCompletionProvider
{
    private readonly object _delegationLock = new();

    /// <summary>
    /// A finished utterance waiting for a delegation to claim it.
    /// </summary>
    private string? _pendingUtterance;

    /// <summary>
    /// A delegation waiting for the user to stop talking.
    /// </summary>
    private string? _pendingDelegationId;

    private IdleFlushTimer? _unclaimedUtteranceTimer;
    private IdleFlushTimer? _staleDelegationTimer;

    private bool IsClientDelegation => LiveSettings.DelegationType == LiveDelegationType.Client;

    /// <summary>
    /// A user turn has settled. Either a delegation is already waiting for it, or it is parked
    /// briefly in case one arrives.
    /// </summary>
    private Task OnUserUtteranceReady(string utterance)
    {
        string? delegationId;

        lock (_delegationLock)
        {
            delegationId = _pendingDelegationId;
            if (delegationId != null)
            {
                _pendingDelegationId = null;
            }
            else
            {
                _pendingUtterance = utterance;
            }
        }

        if (delegationId == null)
        {
            _unclaimedUtteranceTimer?.Touch();
            return Task.CompletedTask;
        }

        _staleDelegationTimer?.Cancel();
        return RunBackendHandler(delegationId, utterance);
    }

    /// <summary>
    /// The model delegated. Either the utterance is already complete, or we wait for it.
    /// </summary>
    private Task OnClientDelegationCreated(string delegationId)
    {
        string? utterance;

        lock (_delegationLock)
        {
            utterance = _pendingUtterance;
            if (utterance != null)
            {
                _pendingUtterance = null;
            }
            else
            {
                _pendingDelegationId = delegationId;
            }
        }

        if (utterance == null)
        {
            _staleDelegationTimer?.Touch();
            return Task.CompletedTask;
        }

        _unclaimedUtteranceTimer?.Cancel();
        return RunBackendHandler(delegationId, utterance);
    }

    /// <summary>
    /// Nobody delegated on this turn, so the voice model handled it alone. The turn still has to
    /// reach the conversation record, which in the delegated path is the routing call's job.
    /// </summary>
    private async Task OnUnclaimedUtterance()
    {
        string? utterance;
        lock (_delegationLock)
        {
            utterance = _pendingUtterance;
            _pendingUtterance = null;
        }

        if (string.IsNullOrEmpty(utterance)) return;

        _logger.LogDebug("No delegation claimed the user turn; recording it without running the backend.");

        await _onInputAudioTranscriptionDone(new RoleDialogModel(AgentRole.User, utterance)
        {
            CurrentAgentId = _conn.CurrentAgentId
        });
    }

    private Task OnStaleDelegation()
    {
        string? delegationId;
        lock (_delegationLock)
        {
            delegationId = _pendingDelegationId;
            _pendingDelegationId = null;
        }

        if (delegationId != null)
        {
            _logger.LogWarning("Delegation {DelegationId} expired without a user utterance to act on.", delegationId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Runs the BotSharp agent as the backend handler and speaks the answer back through the
    /// live conversation. InstructLoop and InstructDirect persist the user message but not the
    /// response, so the conversation record ends up with one user turn here and one assistant
    /// turn from the spoken transcript.
    /// </summary>
    private async Task RunBackendHandler(string delegationId, string utterance)
    {
        _currentDelegationId = delegationId;

        try
        {
            var agentService = _services.GetRequiredService<IAgentService>();
            var convService = _services.GetRequiredService<IConversationService>();
            var routing = _services.GetRequiredService<IRoutingService>();

            var agent = await agentService.LoadAgent(_conn.CurrentAgentId);
            var conversation = await convService.GetConversation(_conn.ConversationId);
            var dialogs = await convService.GetDialogHistory();

            var message = new RoleDialogModel(AgentRole.User, utterance)
            {
                CurrentAgentId = _conn.CurrentAgentId,
                MessageId = Guid.NewGuid().ToString()
            };

            routing.Context.SetDialogs(dialogs);
            routing.Context.SetMessageId(_conn.ConversationId, message.MessageId);

            // In the responses path the hub raises these when the transcript completes.
            var convHooks = _services.GetHooksOrderByPriority<IConversationHook>(_conn.CurrentAgentId);
            foreach (var hook in convHooks)
            {
                hook.SetAgent(agent).SetConversation(conversation);
                await hook.OnMessageReceived(message);
            }

            _logger.LogInformation("Running backend handler for delegation {DelegationId}: {Utterance}",
                delegationId, utterance);

            var response = agent.Type == AgentType.Routing
                ? await routing.InstructLoop(agent, message, dialogs)
                : await routing.InstructDirect(agent, message, dialogs);

            // Routing may have transferred to another agent mid-turn.
            var currentAgentId = routing.Context.GetCurrentAgentId();
            if (!string.IsNullOrEmpty(currentAgentId))
            {
                _conn.CurrentAgentId = currentAgentId;
            }

            await SpeakBackendResult(response?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backend handler failed for delegation {DelegationId}.", delegationId);

            // Leave the model something to work with rather than a silent gap.
            await AppendSessionContext(LiveClientEventType.ThinkingAppend,
                "The backend request failed. Apologize briefly and offer to try again.");
        }
    }

    /// <summary>
    /// Commentary is the channel for content the model paraphrases aloud. AppendSessionContext
    /// keeps it inside the per-append budget, splitting a long answer on sentence boundaries.
    /// </summary>
    private async Task SpeakBackendResult(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogDebug("Backend handler produced no content to speak.");
            return;
        }

        await AppendSessionContext(LiveClientEventType.CommentaryAppend, content);
    }

    private void CreateClientDelegationTimers()
    {
        if (!IsClientDelegation) return;

        var settings = LiveSettings;
        _unclaimedUtteranceTimer = new IdleFlushTimer(
            "unclaimed utterance", TimeSpan.FromMilliseconds(settings.UtteranceClaimMs), OnUnclaimedUtterance, _logger);
        _staleDelegationTimer = new IdleFlushTimer(
            "stale delegation", TimeSpan.FromMilliseconds(settings.DelegationWaitMs), OnStaleDelegation, _logger);
    }

    private void DisposeClientDelegationTimers()
    {
        _unclaimedUtteranceTimer?.Dispose();
        _staleDelegationTimer?.Dispose();

        _unclaimedUtteranceTimer = null;
        _staleDelegationTimer = null;

        lock (_delegationLock)
        {
            _pendingUtterance = null;
            _pendingDelegationId = null;
        }
    }
}
