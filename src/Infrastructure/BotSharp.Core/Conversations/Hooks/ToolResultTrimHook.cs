using System.Text.RegularExpressions;
using BotSharp.Abstraction.Conversations.Settings;

namespace BotSharp.Core.Conversations.Hooks;

/// <summary>
/// Shortens the tool results an older turn replays into the prompt. It runs where history is
/// loaded, so the turn that ran a tool still reads its result whole. Only <c>Content</c> is
/// touched: the call stays intact, and storage keeps the full text.
/// </summary>
public class ToolResultTrimHook : ConversationHookBase
{
    /// <summary>Stands in for a result the assistant message of the same turn already carries.</summary>
    private const string RepliedToUser = "[replied to the user]";

    /// <summary>A brace or bracket that opens a value, as opposed to one in a sentence.</summary>
    private static readonly Regex StructuredStart = new(@"[{\[]\s*[""{\[\d-]", RegexOptions.Compiled);

    private readonly IServiceProvider _services;
    private readonly ConversationSetting _settings;
    private readonly ILogger<ToolResultTrimHook> _logger;

    public ToolResultTrimHook(
        IServiceProvider services,
        ConversationSetting settings,
        ILogger<ToolResultTrimHook> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public override string SelfId => string.Empty;

    public override Task OnDialogsLoaded(List<RoleDialogModel> dialogs)
    {
        var setting = _settings.ToolResultTrim;
        if (!setting.Enable || dialogs.IsNullOrEmpty())
        {
            return base.OnDialogsLoaded(dialogs);
        }

        // A turn is a message id. The current one is left alone for a function that reloads the
        // history mid-turn.
        var currentMessageId = _services.GetRequiredService<IRoutingContext>().MessageId;
        var recentTurns = dialogs
            .Select(x => x.MessageId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .TakeLast(Math.Max(setting.KeepTurns, 0))
            .ToHashSet();

        var replies = dialogs
            .Where(x => x.Role == AgentRole.Assistant && !string.IsNullOrEmpty(x.Content))
            .Select(x => (x.MessageId, x.Content))
            .ToHashSet();

        var trimmed = 0;
        var saved = 0;

        foreach (var dialog in dialogs)
        {
            if (dialog.Role != AgentRole.Function
                || dialog.MessageId == currentMessageId
                || string.IsNullOrEmpty(dialog.Content))
            {
                continue;
            }

            var before = dialog.Content.Length;

            if (replies.Contains((dialog.MessageId, dialog.Content)))
            {
                // Repeating what the assistant message says word for word teaches the model to
                // answer by echoing tool output. Nothing is lost, so recent turns are no exception.
                dialog.Content = RepliedToUser;
            }
            else if (!recentTurns.Contains(dialog.MessageId) && before > setting.MaxLength)
            {
                dialog.Content = Shorten(dialog, setting.MaxLength);
            }
            else
            {
                continue;
            }

            trimmed++;
            saved += before - dialog.Content.Length;
        }

        if (trimmed > 0)
        {
            _logger.LogInformation(
                "[ToolResultTrim] {Count} tool result(s) shortened, {Saved} characters kept out of the prompt.",
                trimmed, saved);
        }

        return base.OnDialogsLoaded(dialogs);
    }

    /// <summary>
    /// Cuts wherever comes first, a structured value or the length cap. Half a document is
    /// something a model can neither read nor tell is incomplete, so one is replaced outright,
    /// while the sentence that introduced it is worth keeping.
    /// </summary>
    private static string Shorten(RoleDialogModel dialog, int maxLength)
    {
        var content = dialog.Content;
        var structure = StructuredStart.Match(content);
        var cut = structure.Success ? Math.Min(structure.Index, maxLength) : maxLength;

        // The call is named in the message right before this one, so the note does not repeat it.
        if (cut == 0)
        {
            return $"[{content.Length} chars omitted; call again for detail]";
        }

        return content[..cut].TrimEnd()
            + $"\r\n... [{content.Length - cut} chars omitted; call again for detail]";
    }
}
