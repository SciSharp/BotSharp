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

public partial class UserService : IUserService
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
            record = db.GetUserByUserName(user.UserName);
        }

        if (record == null && !string.IsNullOrWhiteSpace(user.Phone))
        {
            //if (user.Type != "internal")
            //{
            //    record = db.GetUserByPhoneV2(user.Phone, regionCode: (string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode));
            //}

            record = db.GetUserByPhone(user.Phone, regionCode: (string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode));
        }

        if (record == null && !string.IsNullOrWhiteSpace(user.Email))
        {
            record = db.GetUserByEmail(user.Email);
        }

        if (record != null && record.Verified)
        {
            // account is already activated
            _logger.LogWarning($"User account already exists: {record.Id} {record.UserName}");
            return record;
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

        if (!string.IsNullOrWhiteSpace(user.Password))
        {
            record.Password = Utilities.HashTextMd5($"{user.Password}{record.Salt}");
        }

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

    [SharpCache(10, perInstanceCache: true)]
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
            user = db.GetUserByUserName(_user.UserName);
        }
        else if (_user.Email != null)
        {
            user = db.GetUserByEmail(_user.Email);
        }
        return user;
    }

    [SharpCache(10, perInstanceCache: true)]
    public async Task<User> GetUser(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(id);
        return user;
    }

    [SharpCache(10)]
    public async Task<List<User>> GetUsers(List<string> ids)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var users = db.GetUserByIds(ids);
        return users;
    }

    public async Task<PagedItems<User>> GetUsers(UserFilter filter)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var users = await db.GetUsers(filter);
        return users;
    }

    [SharpCache(10, perInstanceCache: true)]
    public async Task<(bool, User?)> IsAdminUser(string userId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(userId);
        var isAdmin = user != null && UserConstant.AdminRoles.Contains(user.Role);
        return (isAdmin, user);
    }

    public async Task<UserAuthorization> GetUserAuthorizations(IEnumerable<string>? agentIds = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var (isAdmin, user) = await IsAdminUser(_user.Id);
        var auth = new UserAuthorization();

        if (user == null) return auth;

        auth.IsAdmin = isAdmin;
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

    public async Task<bool> VerifyUserNameExisting(string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            return true;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();

        var user = db.GetUserByUserName(userName);
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

    public async Task<List<User>> SearchLoginUsers(User filter)
    {
        if (filter == null)
        {
            return new List<User>();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();

        return db.SearchLoginUsers(filter);
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

    public async Task<bool> SendVerificationCodeNoLogin(User user)
    {
        User? record = await ResetVerificationCode(user);

        if (record == null)
        {
            return false;
        }

        //send code to user Email.
        var hooks = _services.GetServices<IAuthenticationHook>();
        foreach (var hook in hooks)
        {
            await hook.SendVerificationCode(record);
        }

        return true;
    }

    public async Task<User> ResetVerificationCode(User user)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        if (!string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Phone))
        {
            return null;
        }

        User? record = GetLoginUserByUniqueFilter(user, db);

        if (record == null)
        {
            return null;
        }

        record.VerificationCode = Nanoid.Generate(alphabet: "0123456789", size: 6);

        //update current verification code.
        db.UpdateUserVerificationCode(record.Id, record.VerificationCode);

        return record;
    }

    private static User? GetLoginUserByUniqueFilter(User user, IBotSharpRepository db)
    {
        User? record = null;
        if (!string.IsNullOrWhiteSpace(user.Id))
        {
            record = db.GetUserById(user.Id);
        }

        if (record == null && !string.IsNullOrWhiteSpace(user.Phone))
        {
            record = db.GetUserByPhone(user.Phone, regionCode: string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode);
            //if (record == null)
            //{
            //    record = db.GetUserByPhoneV2(user.Phone, regionCode: string.IsNullOrWhiteSpace(user.RegionCode) ? "CN" : user.RegionCode);
            //}
        }

        if (record == null && !string.IsNullOrWhiteSpace(user.Email))
        {
            record = db.GetUserByEmail(user.Email);
        }

        if (record == null && !string.IsNullOrWhiteSpace(user.UserName))
        {
            record = db.GetUserByUserName(user.UserName);
        }

        return record;
    }

    public async Task<bool> SendVerificationCodeLogin()
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
            await hook.SendVerificationCode(record);
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

        User? record = GetLoginUserByUniqueFilter(user, db);

        if (record == null)
        {
            return false;
        }

        if (user.VerificationCode != record.VerificationCode || (record.VerificationCodeExpireAt != null && DateTime.UtcNow > record.VerificationCodeExpireAt))
        {
            return false;
        }

        var newPassword = Utilities.HashTextMd5($"{user.Password}{record.Salt}");
        db.UpdateUserPassword(record.Id, newPassword);
        return true;
    }

    public async Task<bool> SetUserPassword(User user)
    {
        if (!string.IsNullOrEmpty(user.Id) && !string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.Phone))
        {
            return false;
        }
        var db = _services.GetRequiredService<IBotSharpRepository>();

        User? record = GetLoginUserByUniqueFilter(user, db);

        if (record == null)
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

    public async Task<bool> AddDashboardConversation(string conversationId)
    {
        var user = await GetUser(_user.Id);
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.AddDashboardConversation(user?.Id, conversationId);
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> RemoveDashboardConversation(string conversationId)
    {
        var user = await GetUser(_user.Id);
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.RemoveDashboardConversation(user?.Id, conversationId);
        await Task.CompletedTask;
        return true;
    }

    public async Task UpdateDashboardConversation(DashboardConversation newDashConv)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        
        var user = await GetUser(_user.Id);
        var dashConv = db.GetDashboard(user?.Id)?
                         .ConversationList
                         .FirstOrDefault(x => string.Equals(x.ConversationId, newDashConv.ConversationId));
        if (dashConv == null) return;

        dashConv.Name = newDashConv.Name ?? dashConv.Name;
        dashConv.Instruction = newDashConv.Instruction ?? dashConv.Instruction;
        db.UpdateDashboardConversation(user?.Id, dashConv);
        await Task.CompletedTask;
        return;
    }

    public async Task<Dashboard?> GetDashboard()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var user = await GetUser(_user.Id);
        var dash = db.GetDashboard(user?.Id);
        await Task.CompletedTask;
        return dash;
    }
}
