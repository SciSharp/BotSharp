using System.Data;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserResetPasswordModel
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public string VerificationCode { get; set; }

    public User ToUser()
    {
        return new User
        {
            Email = Email,
            Phone = Phone,
            Password = Password,
            VerificationCode = VerificationCode
        };
    }
}
