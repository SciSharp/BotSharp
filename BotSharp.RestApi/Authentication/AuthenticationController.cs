using BotSharp.Core.Accounts;
using DotNetToolkit;
using DotNetToolkit.JwtHelper;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.RestApi.Authentication
{
    /// <summary>
    /// User authentication
    /// </summary>
    public class AuthenticationController : ControllerBase
    {
        /// <summary>
        /// Get user token
        /// </summary>
        /// <param name="userModel"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("/token")]
        public IActionResult Token([FromBody] VmUserLogin userModel)
        {
            if (String.IsNullOrEmpty(userModel.UserName) || String.IsNullOrEmpty(userModel.Password))
            {
                return new BadRequestObjectResult("Username and password should not be empty.");
            }

            var dc = new DefaultDataContextLoader().GetDefaultDc();

            // validate from local
            var user = (from usr in dc.Table<User>()
                        join auth in dc.Table<UserAuth>() on usr.Id equals auth.UserId
                        where usr.Email == userModel.UserName
                        select auth).FirstOrDefault();

            if (user != null)
            {
                // validate password
                string hash = PasswordHelper.Hash(userModel.Password, user.Salt);
                if (user.Password == hash)
                {
                    return Ok(JwtToken.GenerateToken((IConfiguration)AppDomain.CurrentDomain.GetData("Configuration"), user.UserId));
                }
                else
                {
                    return BadRequest("Authorization Failed.");
                }
            }
            else
            {
                return BadRequest("Account doesn't exist");
            }
        }
    }
    
}
