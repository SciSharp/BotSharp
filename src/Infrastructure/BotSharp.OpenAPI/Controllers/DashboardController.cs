using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public async Task<UserDashboardModel> GetComponents(string userId)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var dashboardProfile = await userService.GetDashboard(userId);
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
    #endregion
}
