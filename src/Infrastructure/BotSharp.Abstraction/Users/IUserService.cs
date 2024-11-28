using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users.Models;
using BotSharp.OpenAPI.ViewModels.Users;

namespace BotSharp.Abstraction.Users;

public interface IUserService
{
    Task<User> GetUser(string id);
    Task<PagedItems<User>> GetUsers(UserFilter filter);
    Task<User?> GetUserDetails(string userId, bool includeAgent = false);
    Task<bool> IsAdminUser(string userId);
    Task<UserAuthorization> GetUserAuthorizations(IEnumerable<string>? agentIds = null);
    Task<bool> UpdateUser(User user, bool isUpdateUserAgents = false);
    Task<User> CreateUser(User user);
    Task<Token> ActiveUser(UserActivationModel model);
    Task<Token?> GetAffiliateToken(string authorization);
    Task<Token?> GetAdminToken(string authorization);
    Task<Token?> GetToken(string authorization);
    Task<User> GetMyProfile();
    Task<bool> VerifyUserNameExisting(string userName);
    Task<bool> VerifyEmailExisting(string email);
    Task<bool> VerifyPhoneExisting(string phone, string regionCode);
    Task<bool> SendVerificationCodeResetPasswordNoLogin(User user);
    Task<bool> SendVerificationCodeResetPasswordLogin();
    Task<bool> ResetUserPassword(User user);
    Task<bool> ModifyUserEmail(string email);
    Task<bool> ModifyUserPhone(string phone, string regionCode);
    Task<bool> UpdatePassword(string newPassword, string verificationCode);
    Task<DateTime> GetUserTokenExpires();
    Task<bool> UpdateUsersIsDisable(List<string> userIds, bool isDisable);
    Task<bool> AddDashboardConversation(string userId, string conversationId);
    Task<bool> RemoveDashboardConversation(string userId, string conversationId);
    Task UpdateDashboardConversation(string userId, DashboardConversation dashConv);
    Task<Dashboard?> GetDashboard(string userId);
}