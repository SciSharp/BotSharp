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
    private readonly ILogger _logger;

    public UserService(IServiceProvider services, IUserIdentity user, ILogger<UserService> logger)
    {
        _services = services;
        _user = user;
        _logger = logger;
    }

    public async Task<User> CreateUser(User user)
    {
        if (string.IsNullOrEmpty(user.UserName))
        {
            // generate unique name
            var name = user.Email.Split("@").First() + "-" + Nanoid.Generate("0123456789botsharp", 6);
            user.UserName = name;
        }
        else
        {
            user.UserName = user.UserName.ToLower();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetUserByUserName(user.UserName);

        if (record != null)
        {
            return record;
        }

        if (string.IsNullOrEmpty(user.Id))
        {
            user.Id = Guid.NewGuid().ToString();
        }

        record = user;
        record.Email = user.Email.ToLower();
        record.Salt = Guid.NewGuid().ToString("N");
        record.Password = Utilities.HashText(user.Password, record.Salt);

        db.CreateUser(record);

        Utilities.ClearCache();

        return record;
    }

    public async Task<Token> GetToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password) = base64.SplitAsTuple(":");

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = id.Contains("@") ? db.GetUserByEmail(id) : db.GetUserByUserName(id);
        if (record == null)
        {
            record = db.GetUserByUserName(id);
        }

        if (record == null  || record.Source != "internal")
        {
            // check 3rd party user
            var validators = _services.GetServices<IAuthenticationHook>();
            foreach (var validator in validators)
            {
                var user = await validator.Authenticate(id, password);
                if (user == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(user.Source) || user.Source == "internal")
                {
                    _logger.LogError($"Please set source name in the Authenticate hook.");
                    return null;
                }

                if (record == null)
                {
                    // create a local user record
                    record = new User
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Source = user.Source,
                        ExternalId = user.ExternalId,
                        Password = user.Password,
                    };
                    await CreateUser(record);
                }
                break;
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
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim("source", user.Source),
            new Claim("external_id", user.ExternalId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var validators = _services.GetServices<IAuthenticationHook>();
        foreach (var validator in validators)
        {
            validator.AddClaims(claims);
        }

        var config = _services.GetRequiredService<IConfiguration>();
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var key = Encoding.ASCII.GetBytes(config["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
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
