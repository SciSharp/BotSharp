using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Users;
using System.Text.Json;

namespace BotSharp.Logger.Hooks;

public class InstructionLogHook : InstructHookBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InstructionLogHook> _logger;
    private readonly BotSharpOptions _options;
    private readonly IUserIdentity _user;

    public InstructionLogHook(
        IServiceProvider services,
        ILogger<InstructionLogHook> logger,
        IUserIdentity user,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _user = user;
        _options = options;
    }

    public override async Task OnResponseGenerated(InstructResponseModel response)
    {
        if (response == null) return;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var user = db.GetUserById(_user.Id);
        db.SaveInstructionLogs(new List<InstructionLogModel>
        {
            new InstructionLogModel
            {
                AgentId = response.AgentId,
                Provider = response.Provider,
                Model = response.Model,
                States = state.GetStates(),
                UserId = user?.Id
            }
        });
        return;
    }

    private Dictionary<string, string> CollectStates()
    {
        var res = new Dictionary<string, object>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var curStates = state.GetStates();

        curStates["test"] = JsonSerializer.Serialize(new
        {
            Number = "789",
            Dummy = new
            {
                Id = 123,
                Name = "name",
                Score = 12.123
            },
            Items = new List<object>
            {
                new
                {
                    Name = "image",
                    Label = "before-service",
                    Attribute = new
                    {
                        Location = "Chicago",
                        Time = "afternoon"
                    }
                },
                new
                {
                    Name = "pdf",
                    Label = "after-service",
                    Attribute = new
                    {
                        Location = "New York",
                        Time = "morning"
                    }
                },
            },
            Lists = new List<object>
            {
                "abc",
                "bcd"
            }
        }, _options.JsonSerializerOptions);
        return curStates;
    }
}
