using System.Text.Json;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Core.Agents.Services;
using Xunit;

namespace BotSharp.Core.UnitTests.Routing;

public class RoutingRuleTests
{
    [Fact]
    public void AllowLlmFill_defaults_to_true_for_existing_rules()
    {
        var rule = JsonSerializer.Deserialize<RoutingRule>("""{"field":"department_type","required":true}""");

        Assert.NotNull(rule);
        Assert.True(rule.AllowLlmFill);
    }

    [Fact]
    public void AllowLlmFill_maps_from_json_and_survives_clone()
    {
        var rule = JsonSerializer.Deserialize<RoutingRule>("""{"field":"department_type","required":true,"allow_llm_fill":false}""");

        Assert.NotNull(rule);
        Assert.False(rule.AllowLlmFill);
        Assert.False(rule.Clone().AllowLlmFill);
    }

    [Fact]
    public void MergeRoutingRules_preserves_existing_llm_fill_flag_for_matching_rule()
    {
        var existingRules = new List<RoutingRule>
        {
            new()
            {
                Field = "department_type",
                Type = "data_validation",
                Required = true,
                AllowLlmFill = false
            }
        };
        var incomingRules = new List<RoutingRule>
        {
            new()
            {
                Field = "department_type",
                Type = "data_validation",
                Required = true,
                AllowLlmFill = true
            }
        };

        var mergedRules = MergeRoutingRules(existingRules, incomingRules);

        Assert.False(mergedRules.Single().AllowLlmFill);
    }

    [Fact]
    public void MergeRoutingRules_uses_incoming_llm_fill_flag_for_new_rule()
    {
        var incomingRules = new List<RoutingRule>
        {
            new()
            {
                Field = "new_field",
                Type = "data_validation",
                Required = true,
                AllowLlmFill = false
            }
        };

        var mergedRules = MergeRoutingRules(existingRules: [], incomingRules);

        Assert.False(mergedRules.Single().AllowLlmFill);
    }

    private static List<RoutingRule> MergeRoutingRules(List<RoutingRule>? existingRules, List<RoutingRule>? incomingRules)
    {
        var method = typeof(AgentService).GetMethod("MergeRoutingRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        return Assert.IsType<List<RoutingRule>>(method.Invoke(null, [existingRules, incomingRules]));
    }
}
