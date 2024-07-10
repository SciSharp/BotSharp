using BotSharp.Abstraction.Email.Settings;
using BotSharp.Plugin.EmailHandler.LlmContexts;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net.Http;

namespace BotSharp.Plugin.EmailHandler.Functions
{
    public class HandleEmailRequestFn : IFunctionCallback
    {
        public string Name => "handle_email_request";
        public string Indication => "Handling email request";

        private readonly IServiceProvider _services;
        private readonly ILogger<HandleEmailRequestFn> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _context;
        private readonly BotSharpOptions _options;
        private readonly EmailPluginSettings _emailSettings;

        public HandleEmailRequestFn(IServiceProvider services,
                    ILogger<HandleEmailRequestFn> logger,
                    IHttpClientFactory httpClientFactory,
                    IHttpContextAccessor context,
                    BotSharpOptions options,
                    EmailPluginSettings emailPluginSettings)
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

            try
            {
                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(_emailSettings.Name, _emailSettings.EmailAddress));
                mailMessage.To.Add(new MailboxAddress("", recipient));
                mailMessage.Subject = subject;
                mailMessage.Body = new TextPart("plain")
                {
                    Text = body
                };

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Connect(_emailSettings.SMTPServer, _emailSettings.SMTPPort, SecureSocketOptions.StartTls);
                    smtpClient.Authenticate(_emailSettings.EmailAddress, _emailSettings.Password);
                    smtpClient.Send(mailMessage);
                    smtpClient.Disconnect(true);
                }
                message.Content = $"Email successfully send over to {recipient}. Email Subject: {subject}";
                return true;
            }
            catch (Exception ex)
            {
                var msg = $"Failed to send the email. {ex.Message}";
                _logger.LogWarning($"{msg}\n(Error: {ex.Message})");
                message.Content = msg;
                return false;
            }
        }
    }
}
