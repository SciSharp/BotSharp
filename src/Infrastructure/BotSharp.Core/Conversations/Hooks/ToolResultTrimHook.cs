using System.Text.RegularExpressions;
using BotSharp.Abstraction.Conversations.Settings;

namespace BotSharp.Core.Conversations.Hooks;

/// <summary>
/// Shortens the tool results an older turn replays into the prompt.
/// </summary>
/// <remarks>
/// It runs where history is loaded, which is exactly the turn boundary: the messages a turn
/// produces are appended to the list in memory and never pass through here, so the turn that ran
/// a tool always reads its result whole. Only <c>Content</c> is touched -- the call itself stays
/// intact, so a later turn can still tell that the function already ran, and storage keeps the
/// full text either way.
/// </remarks>
public class ToolResultTrimHook : ConversationHookBase
{
    /// <summary>The opening of a structured document: a brace or bracket that starts one, not a word in prose.</summary>
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

        // A turn is a message id: everything a turn derives carries the id of the message that
        // started it. The current one is skipped as well, because a function that reloads the
        // history mid-turn would otherwise be handed a shortened copy of what it just produced.
        var currentMessageId = _services.GetRequiredService<IRoutingContext>().MessageId;
        var recentTurns = dialogs
            .Select(x => x.MessageId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .TakeLast(Math.Max(setting.KeepTurns, 0))
            .ToHashSet();

        var trimmed = 0;
        var saved = 0;

        foreach (var dialog in dialogs)
        {
            if (dialog.Role != AgentRole.Function
                || dialog.MessageId == currentMessageId
                || recentTurns.Contains(dialog.MessageId)
                || (dialog.Content?.Length ?? 0) <= setting.MaxLength)
            {
                continue;
            }

            var before = dialog.Content.Length;
            dialog.Content = Shorten(dialog, setting.MaxLength);

            trimmed++;
            saved += before - dialog.Content.Length;
        }

        if (trimmed > 0)
        {
            _logger.LogInformation(
                "[ToolResultTrim] {Count} tool result(s) older than {KeepTurns} turn(s) shortened, {Saved} characters kept out of the prompt.",
                trimmed, setting.KeepTurns, saved);
        }

        return base.OnDialogsLoaded(dialogs);
    }

    /// <summary>
    /// Keeps the head of a rendered result, whose useful part comes first, and replaces a
    /// structured one outright: half a JSON document is not something a model can read, and it
    /// cannot tell that the half it got is not the whole.
    /// </summary>
    /// <remarks>
    /// The cut goes wherever comes first: where a structured value begins, or the length cap. A
    /// tool is free to answer with a sentence and then a document -- an MCP server returning
    /// several text blocks does exactly that -- and cutting inside the document would leave a
    /// model something it cannot read and cannot tell is incomplete, while cutting in front of it
    /// keeps the sentence that introduced it. A lone brace in prose is not a document: what is
    /// looked for is a brace or bracket that opens a value.
    /// </remarks>
    private static string Shorten(RoleDialogModel dialog, int maxLength)
    {
        var content = dialog.Content;
        var structure = StructuredStart.Match(content);
        var cut = structure.Success ? Math.Min(structure.Index, maxLength) : maxLength;

        // The call itself sits in the message right before this one, so the note does not name it
        // again -- what a later turn cannot work out on its own is that something was dropped.
        if (cut == 0)
        {
            return $"[{content.Length} chars omitted; call again for detail]";
        }

        return content[..cut].TrimEnd()
            + $"\r\n... [{content.Length - cut} chars omitted; call again for detail]";
    }
}
