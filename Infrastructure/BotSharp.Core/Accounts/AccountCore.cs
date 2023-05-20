using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Accounts
{
    public class AccountCore
    {
        private Database _dc;
        private IConfiguration _config;

        public AccountCore(Database dc = null)
        {
            if (dc == null)
            {
                dc = new DefaultDataContextLoader().GetDefaultDc();
            }
            else
            {
                _dc = dc;
            }

            _config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
        }

        public void CreateUser(User user)
        {
            user.Authenticaiton.IsActivated = false;
            user.Authenticaiton.Salt = PasswordHelper.GetSalt();
            user.Authenticaiton.Password = PasswordHelper.Hash(user.Authenticaiton.Password, user.Authenticaiton.Salt);
            user.Authenticaiton.ActivationCode = Guid.NewGuid().ToString("N");

            _dc.Transaction<IDbRecord>(() =>
            {
                _dc.Table<User>().Add(user);
            });


            $"Created user {user.Email}, user id: {user.Id}".Log(LogLevel.INFO);
        }

        public void Activate(string activationCode)
        {
            var activation = _dc.Table<UserAuth>().FirstOrDefault(x => x.ActivationCode == activationCode && !x.IsActivated);
            if (activation == null)
            {
            }
            else
            {
                _dc.Transaction<IDbRecord>(() =>
                {
                    activation = _dc.Table<UserAuth>().FirstOrDefault(x => x.ActivationCode == activationCode);
                    activation.ActivationCode = String.Empty;
                    activation.IsActivated = true;
                    activation.UpdatedTime = DateTime.UtcNow;
                });
            }
        }
    }
}
