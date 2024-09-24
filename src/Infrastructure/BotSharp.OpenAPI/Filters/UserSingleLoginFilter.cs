using BotSharp.Abstraction.Users.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;

namespace BotSharp.OpenAPI.Filters
{
    public class UserSingleLoginFilter : IAuthorizationFilter
    {
        private readonly IUserService _userService;
        private readonly IServiceProvider _services;

        public UserSingleLoginFilter(IUserService userService, IServiceProvider services)
        {
            _userService = userService;
            _services = services;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var bearerToken = GetBearerToken(context);
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                var config = _services.GetRequiredService<AccountSetting>();
                var token = GetJwtToken(bearerToken);

                if (config.AllowMultipleDeviceLoginUserIds.Contains(token.Claims.First(x => x.Type == "nameid").Value))
                {
                    return;
                }

                var validTo = token.ValidTo.ToLongTimeString();
                var currentExpires = GetUserExpires().ToLongTimeString();

                if (validTo != currentExpires)
                {
                    Serilog.Log.Warning($"Token expired. Token expires at {validTo}, current expires at {currentExpires}");
                    context.Result = new UnauthorizedResult();
                }
            }
        }

        private string GetBearerToken(AuthorizationFilterContext context)
        {
            if (context.HttpContext.Request.Headers.TryGetValue(HeaderNames.Authorization, out var bearerToken)
               && !string.IsNullOrWhiteSpace(bearerToken.ToString()))
            {
                var tokenType = bearerToken.ToString().Split(" ").First();
                if (tokenType == JwtBearerDefaults.AuthenticationScheme)
                {
                    return bearerToken.ToString().Split(" ").Last();
                }
            }
            return null;
        }

        private JwtSecurityToken GetJwtToken(string jwtToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);
            return token;
        }

        private DateTime GetUserExpires()
        {
            return _userService.GetUserTokenExpires().GetAwaiter().GetResult();
        }
    }
}
