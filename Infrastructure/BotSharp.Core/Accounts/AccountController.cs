using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using DotNetToolkit.JwtHelper;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace BotSharp.Core.Accounts
{
    /// <summary>
    /// The /account endpoint is used to create, retrieve, update, and delete account.
    /// </summary>
    public class AccountController : CoreController
    {
        private IConfiguration config;

        public AccountController(IConfiguration config)
        {
            this.config = config;
        }

        [HttpGet("/account")]
        public VmUser GetUser()
        {
            /*var user = dc.Table<User>().Find(CurrentUserId);
            return user.ToObject<VmUser>();*/
            return new VmUser
            {
                Email = "botsharp@ai.com",
                FirstName = "Bot",
                LastName = "Sharp"
            };
        }

        /// <summary>
        /// Get a valid token after login
        /// </summary>
        /// <param name="username">User Email</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("/token")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult Token([FromBody] VmUserLogin userModel)
        {
            if (string.IsNullOrEmpty(userModel.UserName) || string.IsNullOrEmpty(userModel.Password))
            {
                return new BadRequestObjectResult("Username and password should not be empty.");
            }
            return Ok(JwtToken.GenerateToken(config, "botsharp"));
            // validate from local
            var user = (from usr in dc.Table<User>()
                        join auth in dc.Table<UserAuth>() on usr.Id equals auth.UserId
                        where usr.UserName == userModel.UserName
                        select auth).FirstOrDefault();

            if (user != null)
            {
                if (!user.IsActivated)
                {
                    return BadRequest("Account hasn't been activated, please check your email to activate it.");
                }
                else
                {
                    // validate password
                    string hash = PasswordHelper.Hash(userModel.Password, user.Salt);
                    if (user.Password == hash)
                    {
                        return Ok(JwtToken.GenerateToken(config, "botsharp"));
                    }
                    else
                    {
                        return BadRequest("Authorization Failed.");
                    }
                }
            }
            else
            {
                return BadRequest("Account doesn't exist");
            }
        }
    }
}
