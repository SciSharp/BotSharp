using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Authentication
{
    /// <summary>
    /// User login view model
    /// </summary>
    public class VmUserLogin
    {
        /// <summary>
        /// User identity, email or phone
        /// </summary>
        public String UserName { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        public String Password { get; set; }
    }
}
