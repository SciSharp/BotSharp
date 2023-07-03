using BotSharp.Abstraction.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.WeChat
{
    public class WeChatUserIdentity : IUserIdentity
    {

        public string Id => throw new NotImplementedException();

        public string Email => throw new NotImplementedException();

        public string FirstName => throw new NotImplementedException();

        public string LastName => throw new NotImplementedException();
    }
}
