using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.Core.Repository.DbTables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BotSharp.Core.Users.Services;

public class UserService : IUserService
{
    private readonly IServiceProvider _services;
    private readonly ICurrentUser _user;

    public UserService(IServiceProvider services, ICurrentUser user)
    {
        _services = services;
        _user = user;
    }

    public async Task<User> CreateUser(User user)
    {
        var db = _services.GetRequiredService<AgentDbContext>();
        var record = db.User.FirstOrDefault(x => x.Email == user.Email.ToLower());
        if (record != null)
        {
            return record.ToUser();
        }

        record = UserRecord.FromUser(user);
        record.Id = Guid.NewGuid().ToString();
        record.Email = user.Email.ToLower();
        record.Salt = Guid.NewGuid().ToString("N");
        record.Password = Utilities.HashText(user.Password, record.Salt);

        db.Transaction<IAgentTable>(delegate
        {
            db.Add<IAgentTable>(record);
        });

        return record.ToUser();
    }

    public async Task<Token> GetToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (userEmail, password) = base64.SplitAsTuple(":");

        var db = _services.GetRequiredService<AgentDbContext>();
        var record = db.User.FirstOrDefault(x => x.Email == userEmail);
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

    private string GenerateJwtToken(UserRecord user)
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

    public async Task<User> GetMyProfile()
    {
        var userId = _user.Id;

        var db = _services.GetRequiredService<AgentDbContext>();
        var user = (from u in db.User
                    where u.Id == userId
                    select new User
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        CreatedTime = u.CreatedTime,
                        UpdatedTime = u.UpdatedTime,
                        Password = u.Password,
                    }).FirstOrDefault();

        return user;
    }
}
