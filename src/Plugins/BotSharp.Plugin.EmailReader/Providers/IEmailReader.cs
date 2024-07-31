using BotSharp.Plugin.EmailReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailReader.Providers
{
    public interface IEmailReader
    {
        public Task<List<EmailModel>> GetUnreadEmails();
        Task<EmailModel> GetEmailById(string id);
        Task<bool> MarkEmailAsReadById(string id);
    }
}
