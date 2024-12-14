using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Users.Enums;
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
        string hasRegisterId = null;
        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            // generate unique name
            var name = Nanoid.Generate("0123456789botsharp", 10);
            user.UserName = name;
        }
        else
        {
            user.UserName = user.UserName.ToLower();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();

        User? record = null;

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            record = db.GetUserByUserName(user.UserName, regionCode: user.RegionCode);
        }

        if (record != null && record.Verified)
        {
            // account is already activated
            _logger.LogWarning($"User account already exists: {record.Id} {record.UserName}");
            return record;
        }

        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            record = db.GetUserByPhone(user.Phone, regionCode: (string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode));
        }

        if (record == null && !string.IsNullOrWhiteSpace(user.Email))
        {
            record = db.GetUserByEmail(user.Email);
        }

        if (record != null)
        {
            hasRegisterId = record.Id;
        }

        if (string.IsNullOrWhiteSpace(user.Id))
        {
            if (!string.IsNullOrWhiteSpace(hasRegisterId))
            {
                user.Id = hasRegisterId;
            }
            else
            {
                user.Id = Guid.NewGuid().ToString();
            }
        }

        record = user;
        record.Email = user.Email?.ToLower();
        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            //record.Phone = "+" + Regex.Match(user.Phone, @"\d+").Value;
            record.Phone = Regex.Match(user.Phone, @"\d+").Value;
        }
        record.Salt = Guid.NewGuid().ToString("N");
        record.Password = Utilities.HashTextMd5($"{user.Password}{record.Salt}");

        if (_setting.NewUserVerification)
        {
            // record.VerificationCode = Nanoid.Generate(alphabet: "0123456789", size: 6);
            record.Verified = false;
        }

        if (hasRegisterId == null)
        {
            db.CreateUser(record);
        }
        else
        {
            db.UpdateExistUser(hasRegisterId, record);
        }

        _logger.LogWarning($"Created new user account: {record.Id} {record.UserName}, RegionCode: {record.RegionCode}");
        Utilities.ClearCache();

        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.UserCreated(record);
        }

        return record;
    }

    public async Task<bool> UpdatePassword(string password, string verificationCode)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var record = db.GetUserById(_user.Id);

        if (record == null)
        {
            return false;
        }

        if (record.VerificationCode != verificationCode)
        {
            return false;
        }

        var newPassword = Utilities.HashTextMd5($"{password}{record.Salt}");

        db.UpdateUserPassword(record.Id, newPassword);
        return true;
    }

    public async Task<Token?> GetAffiliateToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password, regionCode) = base64.SplitAsTuple(":");
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetAffiliateUserByPhone(id);
        var isCanLogin = record != null && !record.IsDisabled && record.Type == UserType.Affiliate;
        if (!isCanLogin)
        {
            return default;
        }

        if (Utilities.HashTextMd5($"{password}{record.Salt}") != record.Password)
        {
            return default;
        }

        var (token, jwt) = BuildToken(record);

        return await Task.FromResult(token);
    }

    public async Task<Token?> GetAdminToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password, regionCode) = base64.SplitAsTuple(":");
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetUserByPhone(id, type: UserType.Internal);
        var isCanLogin = record != null && !record.IsDisabled
            && record.Type == UserType.Internal && new List<string>
            {
                UserRole.Root,UserRole.Admin
            }.Contains(record.Role);
        if (!isCanLogin)
        {
            return default;
        }

        if (Utilities.HashTextMd5($"{password}{record.Salt}") != record.Password)
        {
            return default;
        }

        var (token, jwt) = BuildToken(record);

        return await Task.FromResult(token);
    }

    private (Token, JwtSecurityToken) BuildToken(User record)
    {
        var accessToken = GenerateJwtToken(record);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var token = new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
        return (token, jwt);
    }

    public async Task<Token?> GetToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password, regionCode) = base64.SplitAsTuple(":");

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = id.Contains("@") ? db.GetUserByEmail(id) : db.GetUserByUserName(id, regionCode: regionCode);
        if (record == null)
        {
            record = db.GetUserByPhone(id, regionCode: regionCode);
        }

        if (record != null && record.Type == UserType.Affiliate)
        {
            return default;
        }

        var hooks = _services.GetServices<IAuthenticationHook>();
        //verify password is correct or not.
        if (record != null && !hooks.Any())
        {
            var hashPassword = Utilities.HashTextMd5($"{password}{record.Salt}");
            if (hashPassword != record.Password)
            {
                return default;
            }
        }

        User? user = record;
        var isAuthenticatedByHook = false;
        if (record == null || record.Source != UserSource.Internal)
        {
            // check 3rd party user
            foreach (var hook in hooks)
            {
                user = await hook.Authenticate(id, password);
                if (user == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(user.Source) || user.Source == UserSource.Internal)
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
                        Type = user.Type,
                        Role = user.Role,
                        RegionCode = user.RegionCode
                    };
                    await CreateUser(record);
                }

                isAuthenticatedByHook = true;
                break;
            }
        }

        if ((hooks.Any() && user == null) || record == null)
        {
            return default;
        }

        if (!isAuthenticatedByHook && _setting.NewUserVerification && !record.Verified)
        {
            return default;
        }

        if (!isAuthenticatedByHook && Utilities.HashTextMd5($"{password}{record.Salt}") != record.Password)
        {
            return default;
        }

        var (token, jwt) = BuildToken(record);
        foreach (var hook in hooks)
        {
            hook.UserAuthenticated(record, token);
        }

        return token;
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user?.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.GivenName, user?.FirstName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.FamilyName, user?.LastName ?? string.Empty),
            new Claim("source", user.Source),
            new Claim("external_id", user.ExternalId ?? string.Empty),
            new Claim("type", user.Type ?? UserType.Client),
            new Claim("role", user.Role ?? UserRole.User),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("phone", user.Phone ?? string.Empty),
            new Claim("affiliate_id", user.AffiliateId ?? string.Empty),
            new Claim("employee_id", user.EmployeeId ?? string.Empty),
            new Claim("regionCode", user.RegionCode ?? "CN")
        };

        var validators = _services.GetServices<IAuthenticationHook>();
        foreach (var validator in validators)
        {
            validator.AddClaims(claims);
        }

        var config = _services.GetRequiredService<IConfiguration>();
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var expireInMinutes = int.Parse(config["Jwt:ExpireInMinutes"] ?? "120");
        var key = Encoding.ASCII.GetBytes(config["Jwt:Key"]);
        var expires = DateTime.UtcNow.AddMinutes(expireInMinutes);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        SaveUserTokenExpiresCache(user.Id, expires, expireInMinutes).GetAwaiter().GetResult();
        return tokenHandler.WriteToken(token);
    }

    private async Task SaveUserTokenExpiresCache(string userId, DateTime expires, int expireInMinutes)
    {
        var config = _services.GetService<IConfiguration>();
        var enableSingleLogin = bool.Parse(config["Jwt:EnableSingleLogin"] ?? "false");
        if (enableSingleLogin)
        {
            var _cacheService = _services.GetRequiredService<ICacheService>();
            await _cacheService.SetAsync(GetUserTokenExpiresCacheKey(userId), expires, TimeSpan.FromMinutes(expireInMinutes));
        }
    }

    private string GetUserTokenExpiresCacheKey(string userId)
    {
        return $"user:{userId}_token_expires";
    }

    public async Task<DateTime> GetUserTokenExpires()
    {
        var _cacheService = _services.GetRequiredService<ICacheService>();
        return await _cacheService.GetAsync<DateTime>(GetUserTokenExpiresCacheKey(_user.Id));
    }

    [MemoryCache(10 * 60, perInstanceCache: true)]
    public async Task<User> GetMyProfile()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        User user = default;

        if (_user.Id != null)
        {
            user = db.GetUserById(_user.Id);
        }
        else if (_user.UserName != null)
        {
            user = db.GetUserByUserName(_user.UserName, _user.RegionCode);
        }
        else if (_user.Email != null)
        {
            user = db.GetUserByEmail(_user.Email);
        }
        return user;
    }

    [MemoryCache(10 * 60, perInstanceCache: true)]
    public async Task<User> GetUser(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(id);
        return user;
    }

    public async Task<PagedItems<User>> GetUsers(UserFilter filter)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var users = db.GetUsers(filter);
        return users;
    }

    public async Task<bool> IsAdminUser(string userId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(userId);
        return user != null && UserConstant.AdminRoles.Contains(user.Role);
    }

    public async Task<UserAuthorization> GetUserAuthorizations(IEnumerable<string>? agentIds = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(_user.Id);
        var auth = new UserAuthorization();

        if (user == null) return auth;

        auth.IsAdmin = UserConstant.AdminRoles.Contains(user.Role);

        var role = db.GetRoles(new RoleFilter { Names = [user.Role] }).FirstOrDefault();
        var permissions = user.Permissions?.Any() == true ? user.Permissions : role?.Permissions ?? [];
        auth.Permissions = permissions;

        if (agentIds == null || !agentIds.Any())
        {
            return auth;
        }

        var userAgents = db.GetUserDetails(user.Id)?.AgentActions?
            .Where(x => agentIds.Contains(x.AgentId) && x.Actions.Any())?.Select(x => new UserAgent
            {
                AgentId = x.AgentId,
                Actions = x.Actions
            }).ToList() ?? [];

        var userAgentIds = userAgents.Select(x => x.AgentId).ToList();
        var roleAgents = db.GetRoleDetails(role?.Id)?.AgentActions?
            .Where(x => !userAgentIds.Contains(x.AgentId))?.Select(x => new UserAgent
            {
                AgentId = x.AgentId,
                Actions = x.Actions
            })?.ToList() ?? [];

        auth.AgentActions = userAgents.Concat(roleAgents);
        return auth;
    }

    public async Task<User?> GetUserDetails(string userId, bool includeAgent = false)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.GetUserDetails(userId, includeAgent);
    }

    public async Task<bool> UpdateUser(User user, bool isUpdateUserAgents = false)
    {
        if (user == null) return false;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.UpdateUser(user, isUpdateUserAgents);
    }

    public async Task<Token> ActiveUser(UserActivationModel model)
    {
        var id = model.UserName;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = id.Contains("@") ? db.GetUserByEmail(id) : db.GetUserByUserName(id, regionCode: (string.IsNullOrWhiteSpace(model.RegionCode) ? "CN" : model.RegionCode));
        if (record == null)
        {
            record = db.GetUserByPhone(id, regionCode: (string.IsNullOrWhiteSpace(model.RegionCode) ? "CN" : model.RegionCode));
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
        {
            return true;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();

        var user = db.GetUserByUserName(userName, regionCode: "CN");
        if (user != null && user.Verified)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> VerifyEmailExisting(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var emailName = db.GetUserByEmail(email);
        if (emailName != null && emailName.Verified)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> VerifyPhoneExisting(string phone, string regionCode)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return true;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var UserByphone = db.GetUserByPhone(phone, regionCode: regionCode);
        if (UserByphone != null && UserByphone.Verified)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> SendVerificationCodeResetPasswordNoLogin(User user)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        User? record = null;

        if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.Phone))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            record = db.GetUserByPhone(user.Phone, regionCode: user.RegionCode);
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            record = db.GetUserByEmail(user.Email);
        }

        if (record == null)
        {
            return false;
        }

        record.VerificationCode = Nanoid.Generate(alphabet: "0123456789", size: 6);

        //update current verification code.
        db.UpdateUserVerificationCode(record.Id, record.VerificationCode);

        //send code to user Email.
        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.VerificationCodeResetPassword(record);
        }

        return true;
    }

    public async Task<bool> SendVerificationCodeResetPasswordLogin()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        User? record = null;

        if (!string.IsNullOrWhiteSpace(_user.Id))
        {
            record = db.GetUserById(_user.Id);
        }

        if (record == null)
        {
            return false;
        }

        record.VerificationCode = Nanoid.Generate(alphabet: "0123456789", size: 6);

        //update current verification code.
        db.UpdateUserVerificationCode(record.Id, record.VerificationCode);

        //send code to user Email.
        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.VerificationCodeResetPassword(record);
        }

        return true;
    }

    public async Task<bool> ResetUserPassword(User user)
    {
        if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.Phone))
        {
            return false;
        }
        var db = _services.GetRequiredService<IBotSharpRepository>();

        User? record = null;

        if (!string.IsNullOrEmpty(user.Email))
        {
            record = db.GetUserByEmail(user.Email);
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            record = db.GetUserByPhone(user.Phone, regionCode: (string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode));
        }

        if (record == null)
        {
            return false;
        }

        if (user.VerificationCode != record.VerificationCode)
        {
            return false;
        }

        var newPassword = Utilities.HashTextMd5($"{user.Password}{record.Salt}");
        db.UpdateUserPassword(record.Id, newPassword);
        return true;
    }

    public async Task<bool> ModifyUserEmail(string email)
    {
        var curUser = await GetMyProfile();
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetUserById(curUser.Id);
        var existEmail = db.GetUserByEmail(email);
        if (record == null || existEmail != null)
        {
            return false;
        }

        record.Email = email;
        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.UserUpdating(record);
        }

        db.UpdateUserEmail(record.Id, record.Email);
        return true;
    }

    public async Task<bool> ModifyUserPhone(string phone, string regionCode)
    {
        if (string.IsNullOrWhiteSpace(regionCode))
        {
            throw new Exception("regionCode is required");
        }
        var curUser = await GetMyProfile();
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetUserById(curUser.Id);
        var existPhone = db.GetUserByPhone(phone, regionCode: regionCode);

        if (record == null || (existPhone != null && existPhone.RegionCode == regionCode))
        {
            return false;
        }

        record.Phone = phone;
        record.RegionCode = regionCode;
        record.UserName = phone;
        record.FirstName = phone;

        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.UserUpdating(record);
        }

        db.UpdateUserPhone(record.Id, record.Phone, regionCode);

        return true;
    }

    public async Task<bool> UpdateUsersIsDisable(List<string> userIds, bool isDisable)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.UpdateUsersIsDisable(userIds, isDisable);

        if (!isDisable)
        {
            return true;
        }

        // del membership
        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.DelUsers(userIds);
        }
        return true;
    }

    public async Task<bool> AddDashboardConversation(string userId, string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.AddDashboardConversation(userId, conversationId);

        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> RemoveDashboardConversation(string userId, string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.RemoveDashboardConversation(userId, conversationId);

        await Task.CompletedTask;
        return true;
    }

    public async Task UpdateDashboardConversation(string userId, DashboardConversation newDashConv)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dashConv = db.GetDashboard(userId)?.ConversationList.FirstOrDefault(x => string.Equals(x.ConversationId, newDashConv.ConversationId));
        if (dashConv == null) return;
        dashConv.Name = newDashConv.Name ?? dashConv.Name;
        dashConv.Instruction = newDashConv.Instruction ?? dashConv.Instruction;
        db.UpdateDashboardConversation(userId, dashConv);
        await Task.CompletedTask;
        return;
    }

    public async Task<Dashboard?> GetDashboard(string userId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dash = db.GetDashboard();
        await Task.CompletedTask;
        return dash;
    }
}
