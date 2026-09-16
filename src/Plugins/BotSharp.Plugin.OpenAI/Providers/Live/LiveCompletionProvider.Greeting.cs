using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Users;

namespace BotSharp.Plugin.OpenAI.Providers.Live;

public partial class LiveCompletionProvider
{
    /// <summary>
    /// Opens the call, so the caller hears something before they have said anything.
    ///
    /// The greeting goes out as commentary, the channel the model speaks from. Putting it in
    /// session.instructions would work once and then stay there, leaving a standing order to
    /// greet for the rest of the call. Nothing is written to the conversation here either: the
    /// model speaks it, the transcript comes back, and the normal turn flush records it like
    /// any other thing it said.
    /// </summary>
    private async Task SendGreeting()
    {
        if (!LiveSettings.GreetOnStart)
        {
            return;
        }

        string? greeting;
        try
        {
            greeting = await ResolveGreeting();
        }
        catch (Exception ex)
        {
            // A missing template or a template that fails to render is not worth losing the call.
            _logger.LogError(ex, "Failed to resolve the {Provider} greeting.", Provider);
            return;
        }

        if (string.IsNullOrWhiteSpace(greeting))
        {
            return;
        }

        _logger.LogInformation("{Provider} opening the call with: {Greeting}", Provider, greeting);
        await AppendSessionContext(LiveClientEventType.CommentaryAppend, greeting);
    }

    /// <summary>
    /// The agent's own ".welcome" template wins, rendered the way the text chat renders it, so
    /// each agent keeps its own opening line. The configured greeting is the fallback for agents
    /// that have no template of their own.
    /// </summary>
    private async Task<string?> ResolveGreeting()
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(_conn.CurrentAgentId);

        var template = agent?.Templates?.FirstOrDefault(x => x.Name == ".welcome");
        if (template != null && !string.IsNullOrWhiteSpace(template.Content))
        {
            var render = _services.GetRequiredService<ITemplateRender>();
            var user = _services.GetService<IUserIdentity>();
            var content = render.Render(template.Content, new Dictionary<string, object>
            {
                { "user", user! }
            });

            var spoken = ToSpeakableText(content);
            if (!string.IsNullOrWhiteSpace(spoken))
            {
                return spoken;
            }
        }

        return LiveSettings.Greeting;
    }

    /// <summary>
    /// A welcome template may hold rich content rather than plain prose. Only the text of it can
    /// be spoken, so buttons and cards are reduced to their wording and the rest dropped.
    /// </summary>
    private string ToSpeakableText(string content)
    {
        try
        {
            var richContentService = _services.GetRequiredService<IRichContentService>();
            var messages = richContentService.ConvertToMessages(content);

            var spoken = string.Join(" ", messages
                .Select(x => x?.Text)
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Trim();

            return !string.IsNullOrWhiteSpace(spoken) ? spoken : content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the welcome template as rich content; speaking it as plain text.");
            return content;
        }
    }
}
