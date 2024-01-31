using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Users.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NanoidDotNet;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BotSharp.Core.Users.Services;

public class UserService : IUserService
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public UserService(IServiceProvider services, IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    public async Task<User> CreateUser(User user)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetUserByEmail(user.Email);
        if (record != null)
        {
            return record;
        }

        record = user;
        
        record.Email = user.Email.ToLower();
        if (string.IsNullOrEmpty(user.UserName))
        {
            var name = record.Email.Split("@").First() + "-" + Nanoid.Generate("123456789botsharp", 6);
            record.UserName = name;
        }
        else
        {
            record.UserName = user.UserName.ToLower();
        }
        record.Salt = Guid.NewGuid().ToString("N");
        record.Password = Utilities.HashText(user.Password, record.Salt);

        db.CreateUser(record);

        Utilities.ClearCache();

        return record;
    }

    public async Task<Token> GetToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (userEmail, password) = base64.SplitAsTuple(":");

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetUserByEmail(userEmail);
        if (record == null)
        {
            // check 3rd party user
            var validators = _services.GetServices<IAuthenticationHook>();
            foreach (var validator in validators)
            {
                var user = await validator.Authenticate(userEmail, password);
                if (user != null)
                {
                    // create a local user record
                    record = new User
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Source = user.Source,
                        ExternalId = user.ExternalId
                    };
                    await CreateUser(record);
                    break;
                }
            }
        }

        if (record == null)
        {
            return default;
        }

        if (Utilities.HashText(password, record.Salt) != record.Password)
        {
            return default;
        }

        var accessToken = GenerateJwtToken(record);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
    }

    private string GenerateJwtToken(User user)
    {
        var config = _services.GetRequiredService<IConfiguration>();
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var key = Encoding.ASCII.GetBytes(config["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
             }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [MemoryCache(10 * 60, perInstanceCache: true)]
    public async Task<User> GetMyProfile()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(_user.Id);
        return user;
    }

    [MemoryCache(10 * 60)]
    public async Task<User> GetUser(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(id);
        return user;
    }
}
