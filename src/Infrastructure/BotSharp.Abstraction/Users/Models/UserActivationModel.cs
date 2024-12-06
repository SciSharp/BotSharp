namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserActivationModel
{
    public string UserName { get; set; }
    public string VerificationCode { get; set; }
    public string RegionCode { get; set; } = "CN";
}
