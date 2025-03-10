using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users.Models;
using BotSharp.OpenAPI.ViewModels.Users;

namespace BotSharp.Abstraction.Users;

public interface IUserService
{
    Task<User> GetUser(string id);
    Task<PagedItems<User>> GetUsers(UserFilter filter);
    Task<List<User>> SearchLoginUsers(User filter);
    Task<User?> GetUserDetails(string userId, bool includeAgent = false);
    Task<(bool, User?)> IsAdminUser(string userId);
    Task<UserAuthorization> GetUserAuthorizations(IEnumerable<string>? agentIds = null);
    Task<bool> UpdateUser(User user, bool isUpdateUserAgents = false);
    Task<User> CreateUser(User user);
    Task<Token> ActiveUser(UserActivationModel model);
    Task<Token?> GetAffiliateToken(string authorization);
    Task<Token?> GetAdminToken(string authorization);
    Task<Token?> GetToken(string authorization);
    Task<Token> CreateTokenByUser(User user);
    Task<Token> RenewToken();
    Task<User> GetMyProfile();
    Task<bool> VerifyUserNameExisting(string userName);
    Task<bool> VerifyEmailExisting(string email);
    Task<bool> VerifyPhoneExisting(string phone, string regionCode);
    Task<User> ResetVerificationCode(User user);
    Task<bool> SendVerificationCodeNoLogin(User user);
    Task<bool> SendVerificationCodeLogin();
    Task<bool> SetUserPassword(User user);
    Task<bool> ResetUserPassword(User user);
    Task<bool> ModifyUserEmail(string email);
    Task<bool> ModifyUserPhone(string phone, string regionCode);
    Task<bool> UpdatePassword(string newPassword, string verificationCode);
    Task<DateTime> GetUserTokenExpires();
    Task<bool> UpdateUsersIsDisable(List<string> userIds, bool isDisable);
    Task<bool> AddDashboardConversation(string conversationId);
    Task<bool> RemoveDashboardConversation(string conversationId);
    Task UpdateDashboardConversation(DashboardConversation dashConv);
    Task<Dashboard?> GetDashboard();
}