using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.EmailReader.LlmContexts;
using BotSharp.Plugin.EmailReader.Models;
using BotSharp.Plugin.EmailReader.Providers;
using BotSharp.Plugin.EmailReader.Settings;
using BotSharp.Plugin.EmailReader.Templates;
using BusinessCore.Utils;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BotSharp.Plugin.EmailReader.Functions;

public class HandleEmailReaderFn : IFunctionCallback
{
    public string Name => "handle_email_reader";
    public readonly static string PROMPT_SUMMARY = "Provide a text summary of the following content.";
    public readonly static string RICH_CONTENT_SUMMARIZE = "Summarize the particular email by messageId";
    public readonly static string RICH_CONTENT_READ_EMAIL = "Read the email by messageId";
    public readonly static string RICH_CONTENT_MARK_READ = "Mark the email message as read by messageId";
    public string Indication => "Handling email read";
    private readonly IServiceProvider _services;
    private readonly ILogger<HandleEmailReaderFn> _logger;
    private readonly IHttpContextAccessor _context;
    private readonly BotSharpOptions _options;
    private readonly EmailReaderSettings _emailSettings;
    private readonly IConversationStateService _state;
    private readonly IEmailReader _emailProvider;

    public HandleEmailReaderFn(IServiceProvider services,
                ILogger<HandleEmailReaderFn> logger,
                IHttpContextAccessor context,
                BotSharpOptions options,
                EmailReaderSettings emailPluginSettings,
                IConversationStateService state,
                IEmailReader emailProvider)
    {
        _services = services;
        _logger = logger;
        _context = context;
        _options = options;
        _emailSettings = emailPluginSettings;
        _state = state;
        _emailProvider = emailProvider;
    }
    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var isMarkRead = args?.IsMarkRead ?? false;
        var isSummarize = args?.IsSummarize ?? false;
        var messageId = args?.MessageId;
        try
        {
            if (!string.IsNullOrEmpty(messageId))
            {
                if (isMarkRead)
                {
                    await _emailProvider.MarkEmailAsReadById(messageId);
                    message.Content = $"The email message has been marked as read.";
                    return true;
                }
                var emailMessage = await _emailProvider.GetEmailById(messageId);
                if (isSummarize)
                {
                    var prompt = $"{PROMPT_SUMMARY} The content was sent by {emailMessage.From.ToString()}. Details: {emailMessage.TextBody}";
                    var agent = new Agent
                    {
                        Id = BuiltInAgentId.UtilityAssistant,
                        Name = "Utility Assistant",
                        Instruction = prompt
                    };

                    var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
                    var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == "openai");
                    var model = llmProviderService.GetProviderModel(provider: provider, id: "gpt-4");
                    var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model.Name);
                    var convService = _services.GetService<IConversationService>();
                    var conversationId = convService.ConversationId;
                    var dialogs = convService.GetDialogHistory(fromBreakpoint: false);
                    var response = await completion.GetChatCompletions(agent, dialogs);
                    var content = response?.Content ?? string.Empty;
                    message.Content = content;
                    message.RichContent = BuildRichContent.TextPostBackRichContent(_state.GetConversationId(), message.Content);
                    return true;
                }
                UniqueId.TryParse(messageId, out UniqueId uid);
                message.RichContent = BuildRichContentForEmail(emailMessage, uid.ToString());
                return true;
            }
            var emails = await _emailProvider.GetUnreadEmails();
            message.Content = "Please choose which one to read for you.";
            message.RichContent = BuildRichContentForSubject(emails.OrderByDescending(x => x.CreateDate).ToList());
            return true;
        }
        catch (Exception ex)
        {
            var msg = $"Failed to read the emails. {ex.Message}";
            _logger.LogError($"{msg}\n(Error: {ex.Message})");
            message.Content = msg;
            return false;
        }
    }
    private RichContent<IRichMessage> BuildRichContentForSubject(List<EmailModel> emailSubjects)
    {
        var text = "Please let me know which message I need to read?";

        return new RichContent<IRichMessage>
        {
            FillPostback = true,
            Editor = EditorTypeEnum.None,
            Recipient = new Recipient
            {
                Id = _state.GetConversationId()
            },
            Message = new GenericTemplateMessage<EmailSubjectElement>
            {
                Text = text,
                Elements = GetElements(emailSubjects)
            }
        };
    }
    private RichContent<IRichMessage> BuildRichContentForEmail(EmailModel email, string uid)
    {
        var text = "The email details are given below. \n";

        return new RichContent<IRichMessage>
        {
            FillPostback = true,
            Editor = EditorTypeEnum.None,
            Recipient = new Recipient
            {
                Id = _state.GetConversationId()
            },
            Message = new GenericTemplateMessage<EmailSubjectElement>
            {
                Text = $"{text}<b>From</b>: {email.From.ToString()}\n<b>Subject</b>: {email.Subject}\n{email.Body}",
                Elements = GetElements(uid)
            }
        };
    }
    private static List<EmailSubjectElement> GetElements(string uid)
    {
        var element = new EmailSubjectElement()
        {
            Buttons = new ElementButton[]
            {
               BuildMarkReadElementButton(uid)
            }
        };
        return new List<EmailSubjectElement>() { element };
    }
    private static List<EmailSubjectElement> GetElements(List<EmailModel> emails)
    {
        var elements = emails.Select(e => new EmailSubjectElement
        {
            Title = $"Subject: {e.Subject}",
            Subtitle = $"From: {e.From}<br/> Date: {e.CreateDate}",
            Buttons = BuildElementButton(e)
        }).ToList();
        return elements;
    }
    private static ElementButton[] BuildElementButton(EmailModel email)
    {
        var elements = new List<ElementButton>() { };
        elements.Add(new ElementButton
        {
            Title = "Read",
            Payload = $"{RICH_CONTENT_READ_EMAIL}: {email.UId}.",
            Type = "text",
        });
        elements.Add(new ElementButton
        {
            Title = "Summarize",
            Payload = $"{RICH_CONTENT_SUMMARIZE}: {email.UId}.",
            Type = "text",
        });
        return elements.ToArray();
    }
    private static ElementButton BuildMarkReadElementButton(string uId)
    {
        return new ElementButton
        {
            Title = "Mark as read",
            Payload = $"{RICH_CONTENT_MARK_READ}: {uId}.",
            Type = "text",
        };
    }
}
