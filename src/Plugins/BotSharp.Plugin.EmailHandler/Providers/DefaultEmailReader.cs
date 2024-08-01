using BotSharp.Plugin.EmailHandler.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailHandler.Providers
{
    public class DefaultEmailReader : IEmailReader
    {
        public EmailReaderSettings _emailReaderSettings;
        public const int MAX_UNREAD_COUNT = 5;
        public DefaultEmailReader(EmailReaderSettings emailReaderSettings)
        {
            _emailReaderSettings = emailReaderSettings;
        }
        public async Task<ImapClient> GetImapClient()
        {
            var client = new ImapClient();
            await client.ConnectAsync(_emailReaderSettings.IMAPServer, _emailReaderSettings.IMAPPort, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_emailReaderSettings.Username, _emailReaderSettings.Password);
            return client;
        }
        public async Task<List<EmailModel>> GetUnreadEmails()
        {
            var emails = new List<EmailModel>();
            using var client = await GetImapClient();
            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
            var query = SearchQuery.NotSeen;
            var result = await client.Inbox.SearchAsync(query);
            var uIds = result.TakeLast(MAX_UNREAD_COUNT);
            foreach (var uid in uIds)
            {
                var inboxMsg = await client.Inbox.GetMessageAsync(uid);
                emails.Add(new EmailModel()
                {
                    Subject = inboxMsg.Subject,
                    CreateDate = inboxMsg.Date.UtcDateTime,
                    From = FormatEmailAddress(inboxMsg.From.ToString()),
                    UId = uid.ToString()

                });
            }
            await client.DisconnectAsync(true);
            return emails;
        }
        public string FormatEmailAddress(string emailAddress)
        {
            string pattern = "\"([^\"]+)\"\\s*<([^>]+)>";

            var match = Regex.Match(emailAddress, pattern);
            if (match.Success)
            {
                string name = match.Groups[1].Value;
                string email = match.Groups[2].Value;
                string result = $"{name} {email}";
                return result;
            }
            return emailAddress;
        }
        public async Task<EmailModel> GetEmailById(string id)
        {

            UniqueId.TryParse(id, out UniqueId uid);
            using var client = await GetImapClient();
            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
            var message = await client.Inbox.GetMessageAsync(uid);
            return new EmailModel()
            {
                CreateDate = message.Date.UtcDateTime,
                From = FormatEmailAddress(message.From.ToString()),
                Subject = message.Subject,
                UId = uid.ToString(),
                Body = message.HtmlBody,
                TextBody = message.TextBody
            };
        }

        public async Task<bool> MarkEmailAsReadById(string id)
        {
            try
            {
                UniqueId.TryParse(id, out UniqueId uid);
                using var client = await GetImapClient();
                await client.Inbox.OpenAsync(FolderAccess.ReadWrite);
                await client.Inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
