using AdaptiveCards;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// Maps a BotSharp <see cref="RoleDialogModel"/> reply to a Teams-renderable activity.
/// Plain text becomes a text activity; rich content becomes an Adaptive Card so buttons /
/// quick replies survive in Teams (which does not reliably support suggestedActions).
/// </summary>
public class AdaptiveCardConverter
{
    private static readonly AdaptiveSchemaVersion Schema = new(1, 4);

    public IActivity Convert(RoleDialogModel message)
    {
        var rich = message.RichContent?.Message;

        switch (rich)
        {
            case QuickReplyMessage quickReply:
                return BuildCard(quickReply.Text, quickReply.QuickReplies.Select(q =>
                    (AdaptiveAction)new AdaptiveSubmitAction
                    {
                        Title = q.Title,
                        Data = new { payload = q.Payload ?? q.Title }
                    }));

            case ButtonTemplateMessage buttonTemplate:
                return BuildCard(buttonTemplate.Text, buttonTemplate.Buttons.Select(ToAction));

            case TextMessage textMessage:
                return MessageFactory.Text(textMessage.Text);

            case not null when !string.IsNullOrEmpty(rich.Text):
                return MessageFactory.Text(rich.Text);

            default:
                return MessageFactory.Text(message.Content ?? string.Empty);
        }
    }

    private static AdaptiveAction ToAction(ElementButton button)
    {
        if (string.Equals(button.Type, "web_url", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(button.Url))
        {
            return new AdaptiveOpenUrlAction { Title = button.Title, Url = new Uri(button.Url) };
        }

        return new AdaptiveSubmitAction
        {
            Title = button.Title,
            Data = new { payload = button.Payload ?? button.Title }
        };
    }

    private static IActivity BuildCard(string text, IEnumerable<AdaptiveAction> actions)
    {
        var card = new AdaptiveCard(Schema);
        if (!string.IsNullOrEmpty(text))
        {
            card.Body.Add(new AdaptiveTextBlock { Text = text, Wrap = true });
        }
        card.Actions.AddRange(actions);

        return MessageFactory.Attachment(new Microsoft.Agents.Core.Models.Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        });
    }
}
