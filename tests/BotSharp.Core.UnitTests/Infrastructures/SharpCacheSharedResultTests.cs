using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Tasks;
using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.Utilities;
using BotSharp.Core.Routing.Hooks;
using BotSharp.OpenAPI.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Infrastructures;

/// <summary>
/// Once SharpCache keys are content-based (GTR-13556), <c>GetRoutableAgents</c> and <c>GetAgents</c> hand the
/// same cached objects to every caller. These pin that the two callers which used to modify what they got
/// back now work on copies, so one conversation or request cannot change what the next one sees.
/// </summary>
public class SharpCacheSharedResultTests
{
    [Fact]
    public async Task RoutingAgentHook_removes_filled_fields_from_a_copy_not_from_the_cached_routable_agents()
    {
        // What the cache returns: the same array instance on every call.
        var cached = new[]
        {
            new RoutableAgent
            {
                AgentId = "task-1",
                Name = "task-1",
                Type = AgentType.Task,
                RequiredFields = [new ParameterPropertyDef("wo_id", "work order id"), new ParameterPropertyDef("client_id", "client id")],
                OptionalFields = [new ParameterPropertyDef("note", "note")]
            }
        };

        // Conversation A already has every field filled; conversation B has none of them.
        var dictA = await RunRoutingHook(cached, filledStates: ["wo_id", "client_id", "note"]);
        var dictB = await RunRoutingHook(cached, filledStates: []);

        var agentA = ((RoutableAgent[])dictA["routing_agents"]).Single();
        var agentB = ((RoutableAgent[])dictB["routing_agents"]).Single();

        Assert.Empty(agentA.RequiredFields);
        Assert.Empty(agentA.OptionalFields);
        Assert.Equal(["wo_id", "client_id"], agentB.RequiredFields.Select(x => x.Name));
        Assert.Equal(["note"], agentB.OptionalFields.Select(x => x.Name));

        // The cached entry itself is untouched.
        Assert.Equal(2, cached[0].RequiredFields.Count);
        Assert.Single(cached[0].OptionalFields);
    }

    [Fact]
    public async Task AgentController_GetAgent_does_not_modify_the_cached_agent()
    {
        var cachedAgent = new Agent
        {
            Id = "child",
            Name = "child",
            InheritAgentId = "parent",
            Templates = [new AgentTemplate("own", "own template")],
            Functions = [new FunctionDef { Name = "own_fn" }],
            RoutingRules = [new RoutingRule { Field = "wo_id", RedirectTo = "redirect" }]
        };
        var redirectAgent = new Agent { Id = "redirect", Name = "Redirect Agent" };

        var agentService = new Mock<IAgentService>();
        agentService.Setup(x => x.GetAgents(It.Is<AgentFilter>(f => f.AgentIds!.Contains("child"))))
            .ReturnsAsync(new PagedItems<Agent> { Items = [cachedAgent], Count = 1 });
        agentService.Setup(x => x.GetAgents(It.Is<AgentFilter>(f => f.AgentIds!.Contains("redirect"))))
            .ReturnsAsync(new PagedItems<Agent> { Items = [redirectAgent], Count = 1 });
        // Mirrors the real InheritAgent: appends the parent's templates and functions.
        agentService.Setup(x => x.InheritAgent(It.IsAny<Agent>()))
            .Callback((Agent agent) =>
            {
                agent.Templates.Add(new AgentTemplate("inherited", "parent template"));
                agent.Functions.Add(new FunctionDef { Name = "inherited_fn" });
            })
            .Returns(Task.CompletedTask);

        var userService = new Mock<IUserService>();
        userService.Setup(x => x.GetUserAuthorizations(It.IsAny<IEnumerable<string>?>())).ReturnsAsync(new UserAuthorization());

        var services = new ServiceCollection();
        services.AddSingleton(new AgentSettings());
        services.AddSingleton(userService.Object);
        var controller = new AgentController(agentService.Object, Mock.Of<IAgentTaskService>(), Mock.Of<IUserIdentity>(), services.BuildServiceProvider());

        var view = await controller.GetAgent("child");

        // The response carries the inherited items and the resolved redirect name...
        Assert.Equal(["own", "inherited"], view!.Templates.Select(x => x.Name));
        Assert.Equal(["own_fn", "inherited_fn"], view.Functions.Select(x => x.Name));
        Assert.Equal("Redirect Agent", view.RoutingRules.Single().RedirectToAgentName);

        // ...but none of it leaked into the agent other callers get from the cache.
        Assert.Equal(["own"], cachedAgent.Templates.Select(x => x.Name));
        Assert.Equal(["own_fn"], cachedAgent.Functions.Select(x => x.Name));
        Assert.Null(cachedAgent.RoutingRules.Single().RedirectToAgentName);
    }

    private static async Task<IDictionary<string, object>> RunRoutingHook(RoutableAgent[] cached, string[] filledStates)
    {
        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.GetRoutableAgents(It.IsAny<List<string>>())).ReturnsAsync(cached);

        var states = new Mock<IConversationStateService>();
        states.Setup(x => x.ContainsState(It.IsAny<string>())).Returns((string name) => filledStates.Contains(name));

        var services = new ServiceCollection();
        services.AddSingleton(routing.Object);
        services.AddSingleton(states.Object);

        var hook = new RoutingAgentHook(services.BuildServiceProvider(), new AgentSettings(), new RoutingSettings());
        hook.SetAgent(new Agent { Id = "router", Name = "router", Type = AgentType.Routing });

        var dict = new Dictionary<string, object>();
        await hook.OnInstructionLoaded("template", dict);
        return dict;
    }
}
