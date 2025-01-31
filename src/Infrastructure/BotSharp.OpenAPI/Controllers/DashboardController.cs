using BotSharp.Abstraction.Options;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public DashboardController(IServiceProvider services,
        IUserIdentity user,
        BotSharpOptions options)
    {
        _services = services;
        _user = user;

    }
    #region User Components
    [HttpGet("/dashboard/components")]
    public async Task<UserDashboardModel> GetComponents()
    {
        var userService = _services.GetRequiredService<IUserService>();
        var dashboardProfile = await userService.GetDashboard();
        if (dashboardProfile == null) return new UserDashboardModel();

        var result = new UserDashboardModel
        {
            ConversationList = dashboardProfile.ConversationList.Select(
                x => new UserDashboardConversationModel
                {
                    Name = x.Name,
                    ConversationId = x.ConversationId,
                    Instruction = x.Instruction
                }
            ).ToList()
        };
        return result;
    }

    [HttpPost("/dashboard/component/conversation")]
    public async Task UpdateDashboardConversationInstruction(UserDashboardConversationModel dashConv)
    {
        if (string.IsNullOrEmpty(dashConv.Name) && string.IsNullOrEmpty(dashConv.Instruction)) 
        {
            return;    
        }
        var newDashConv = new DashboardConversation
        {
            Id = Guid.Empty.ToString(),
            ConversationId = dashConv.ConversationId
        };
        if (!string.IsNullOrEmpty(dashConv.Name))
        {
            newDashConv.Name = dashConv.Name;
        }
        if (!string.IsNullOrEmpty(dashConv.Instruction))
        {
            newDashConv.Instruction = dashConv.Instruction;
        }

        var userService = _services.GetRequiredService<IUserService>();
        await userService.UpdateDashboardConversation(newDashConv);
        return;
    }
    #endregion
}
