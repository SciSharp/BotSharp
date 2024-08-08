using BotSharp.Abstraction.Files.Utilities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO;

namespace BotSharp.Plugin.EmailHandler.Functions;

public class HandleEmailSenderFn : IFunctionCallback
{
    public string Name => "handle_email_sender";
    public string Indication => "Handling email send request";

    private readonly IServiceProvider _services;
    private readonly ILogger<HandleEmailSenderFn> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _context;
    private readonly BotSharpOptions _options;
    private readonly EmailSenderSettings _emailSettings;

    public HandleEmailSenderFn(
        IServiceProvider services,
        ILogger<HandleEmailSenderFn> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor context,
        BotSharpOptions options,
        EmailSenderSettings emailPluginSettings)
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
            var response = await SendEmailBySMTP(mailMessage);
            message.Content = response;

            _logger.LogWarning($"Email successfully send over to {recipient}. Email Subject: {subject} [{response}]");
            return true;
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send the email. {ex.Message}";
            _logger.LogError($"{msg}\n(Error: {ex.Message}\r\n{ex.InnerException})");
            message.Content = msg;
            return false;
        }
    }

    private async Task<IEnumerable<MessageFileModel>> GetConversationFiles()
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conversationId = convService.ConversationId;

        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var selecteds = await fileInstruct.SelectMessageFiles(conversationId, new SelectFileOptions { IncludeBotFile = true });
        return selecteds;
    }

    private void BuildEmailAttachments(BodyBuilder builder, IEnumerable<MessageFileModel> files)
    {
        if (files.IsNullOrEmpty()) return;

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.FileStorageUrl)) continue;

            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var fileBytes = fileStorage.GetFileBytes(file.FileStorageUrl);
            builder.Attachments.Add($"{file.FileName}.{file.FileType}", fileBytes, ContentType.Parse(file.ContentType));
            Thread.Sleep(100);
        }
    }

    private async Task<string> SendEmailBySMTP(MimeMessage mailMessage)
    {
        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(_emailSettings.SMTPServer, _emailSettings.SMTPPort, SecureSocketOptions.StartTls);
        await smtpClient.AuthenticateAsync(_emailSettings.EmailAddress, _emailSettings.Password);
        var response = await smtpClient.SendAsync(mailMessage);
        return response ?? "Email sent";
    }
}
