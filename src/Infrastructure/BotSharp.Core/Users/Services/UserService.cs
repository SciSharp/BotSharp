using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.Users.Settings;
using BotSharp.OpenAPI.ViewModels.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NanoidDotNet;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Users.Services;

public class UserService : IUserService
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ILogger _logger;
    private readonly AccountSetting _setting;

    public UserService(IServiceProvider services,
        IUserIdentity user,
        ILogger<UserService> logger,
        AccountSetting setting)
    {
        _services = services;
        _user = user;
        _logger = logger;
        _setting = setting;
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
        record.Email = user.Email?.ToLower();
        if (user.Phone != null)
        {
            record.Phone = "+" + Regex.Match(user.Phone, @"\d+").Value;
        }
        record.Salt = Guid.NewGuid().ToString("N");
        record.Password = Utilities.HashText(user.Password, record.Salt);

        if (_setting.NewUserVerification)
        {
            record.VerificationCode = Nanoid.Generate(alphabet: "0123456789", size: 6);
            record.Verified = false;
        }

        db.CreateUser(record);

        _logger.LogWarning($"Created new user account: {record.Id} {record.UserName}");
        Utilities.ClearCache();

        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.UserCreated(record);
        }

        return record;
    }

    public async Task<Token?> GetToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password) = base64.SplitAsTuple(":");

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = id.Contains("@") ? db.GetUserByEmail(id) : db.GetUserByUserName(id);
        if (record == null)
        {
            record = db.GetUserByUserName(id);
        }

        User? user = record;
        var hooks = _services.GetServices<IAuthenticationHook>();
        if (record == null || record.Source != "internal")
        {
            // check 3rd party user
            foreach (var hook in hooks)
            {
                user = await hook.Authenticate(id, password);
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

        if ((!hooks.IsNullOrEmpty() && user == null) || record == null)
        {
            return default;
        }

        if (_setting.NewUserVerification && !record.Verified)
        {
            return default;
        }

#if !DEBUG
        if (Utilities.HashText(password, record.Salt) != record.Password)
        {
            return default;
        }
#endif

        var accessToken = GenerateJwtToken(record);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var token = new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };

        foreach (var hook in hooks)
        {
            hook.BeforeSending(token);
        }

        return token;
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user?.FirstName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.FamilyName, user?.LastName ?? string.Empty),
            new Claim("source", user.Source),
            new Claim("external_id", user.ExternalId ?? string.Empty),
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
                SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [MemoryCache(10 * 60, perInstanceCache: true)]
    public async Task<User> GetMyProfile()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        User user = default;
        if (_user.UserName != null)
        {
            user = db.GetUserByUserName(_user.UserName);
        }
        else if (_user.Email != null)
        {
            user = db.GetUserByEmail(_user.Email);
        }
        return user;
    }

    [MemoryCache(10 * 60)]
    public async Task<User> GetUser(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(id);
        return user;
    }

    public async Task<Token> ActiveUser(UserActivationModel model)
    {
        var id = model.UserName;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = id.Contains("@") ? db.GetUserByEmail(id) : db.GetUserByUserName(id);
        if (record == null)
        {
            record = db.GetUserByUserName(id);
        }

        if (record == null)
        {
            return default;
        }

        if (record.VerificationCode != model.VerificationCode)
        {
            return default;
        }

        if (record.Verified)
        {
            return default;
        }

        db.UpdateUserVerified(record.Id);

        var accessToken = GenerateJwtToken(record);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var token = new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
        return token;
    }

    public async Task<bool> VerifyUserNameExisting(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            return true;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserByUserName(userName);
        if (user != null)
            return true;

        return false;
    }

    public async Task<bool> VerifyEmailExisting(string email)
    {
        if (string.IsNullOrEmpty(email))
            return true;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var emailName = db.GetUserByEmail(email);
        if (emailName != null)
            return true;

        return false;
    }
}
