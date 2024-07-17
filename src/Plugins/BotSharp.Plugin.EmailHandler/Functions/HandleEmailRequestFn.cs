using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO;

namespace BotSharp.Plugin.EmailHandler.Functions;

public class HandleEmailRequestFn : IFunctionCallback
{
    public string Name => "handle_email_request";
    public string Indication => "Handling email request";

    private readonly IServiceProvider _services;
    private readonly ILogger<HandleEmailRequestFn> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _context;
    private readonly BotSharpOptions _options;
    private readonly EmailHandlerSettings _emailSettings;

    public HandleEmailRequestFn(
        IServiceProvider services,
        ILogger<HandleEmailRequestFn> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor context,
        BotSharpOptions options,
        EmailHandlerSettings emailPluginSettings)
    {
        _services = services;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _options = options;
        _emailSettings = emailPluginSettings;
    }
    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var recipient = args?.ToAddress;
        var body = args?.Content;
        var subject = args?.Subject;
        var isNeedAttachments = args?.IsNeedAttachemnts ?? false;
        var bodyBuilder = new BodyBuilder();

        try
        {
            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress(_emailSettings.Name, _emailSettings.EmailAddress));
            mailMessage.To.Add(new MailboxAddress("", recipient));
            mailMessage.Subject = subject;
            bodyBuilder.TextBody = body;

            if (isNeedAttachments)
            {
                var files = await GetConversationFiles();
                BuildEmailAttachments(bodyBuilder, files);
            }

            mailMessage.Body = bodyBuilder.ToMessageBody();
            var response = await HandleSendEmailBySMTP(mailMessage);
            message.Content = response;

            _logger.LogWarning($"Email successfully send over to {recipient}. Email Subject: {subject} [{response}]");
            return true;
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send the email. {ex.Message}";
            _logger.LogError($"{msg}\n(Error: {ex.Message})");
            message.Content = msg;
            return false;
        }
    }

    private async Task<IEnumerable<MessageFileModel>> GetConversationFiles()
    {
        var convService = _services.GetService<IConversationService>();
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var conversationId = convService.ConversationId;
        var dialogs = convService.GetDialogHistory(fromBreakpoint: false);
        var messageIds = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var files = fileService.GetMessageFiles(conversationId, messageIds, FileSourceType.User);
        return await SelectFiles(files, dialogs);
    }

    private async Task<IEnumerable<MessageFileModel>> SelectFiles(IEnumerable<MessageFileModel> files, List<RoleDialogModel> dialogs)
    {
        if (files.IsNullOrEmpty()) return new List<MessageFileModel>();

        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        try
        {
            var promptFiles = files.Select((x, idx) =>
            {
                return $"id: {idx + 1}, file_name: {x.FileName}.{x.FileType}, content_type: {x.ContentType}";
            }).ToList();
            var prompt = db.GetAgentTemplate(BuiltInAgentId.UtilityAssistant, "email_attachment_prompt");
            prompt = render.Render(prompt, new Dictionary<string, object>
            {
                { "file_list", promptFiles }
            });

            var agent = new Agent
            {
                Id = BuiltInAgentId.UtilityAssistant,
                Name = "Utility Assistant",
                Instruction = prompt
            };

            var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == "openai");
            var model = llmProviderService.GetProviderModel(provider: provider, id: "gpt-4");
            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider, model: model.Name);
            var response = await completion.GetChatCompletions(agent, dialogs);
            var content = response?.Content ?? string.Empty;
            var fids = JsonSerializer.Deserialize<List<int>>(content) ?? new List<int>();
            return files.Where((x, idx) => fids.Contains(idx + 1)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting the email file response. {ex.Message}\r\n{ex.InnerException}");
            return new List<MessageFileModel>();
        }
    }

    private void BuildEmailAttachments(BodyBuilder builder, IEnumerable<MessageFileModel> files)
    {
        if (files.IsNullOrEmpty()) return;

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.FileStorageUrl)) continue;

            using var fs = File.OpenRead(file.FileStorageUrl);
            var binary = BinaryData.FromStream(fs);
            builder.Attachments.Add($"{file.FileName}.{file.FileType}", binary.ToArray(), ContentType.Parse(file.ContentType));
            fs.Close();
            Thread.Sleep(100);
        }
    }

    private async Task<string> HandleSendEmailBySMTP(MimeMessage mailMessage)
    {
        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(_emailSettings.SMTPServer, _emailSettings.SMTPPort, SecureSocketOptions.StartTls);
        await smtpClient.AuthenticateAsync(_emailSettings.EmailAddress, _emailSettings.Password);
        var response = await smtpClient.SendAsync(mailMessage);
        return response;
    }
}
