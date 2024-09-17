using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;

namespace BotSharp.OpenAPI.Filters
{
    public class UserSignleAccountFilter : IAuthorizationFilter
    {
        private readonly IUserService _userService;

        public UserSignleAccountFilter(IUserService userService)
        {
            _userService = userService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var bearerToken = GetBearerToken(context);
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                if (GetJwtTokenExpires(bearerToken).ToLongTimeString() != GetUserExpires().ToLongTimeString())
                {
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

        private DateTime GetJwtTokenExpires(string jwtToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);
            return token.ValidTo;
        }

        private DateTime GetUserExpires()
        {
            return _userService.GetUserTokenExpires().GetAwaiter().GetResult();
        }
    }
}
