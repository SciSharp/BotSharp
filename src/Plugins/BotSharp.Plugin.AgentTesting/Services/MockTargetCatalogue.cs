using BotSharp.Abstraction.Functions.Models;
using System.Text;
using System.Text.Json;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// The functions an agent can actually call, derived live from the agent definition.
///
/// Two shapes of the same list, deliberately kept together so they cannot drift:
/// <see cref="Names"/> is what GET /agent-test/mock-targets returns to the case editor, and
/// <see cref="Describe"/> is the richer form <see cref="ICaseAuthor"/> puts in front of a model.
///
/// Derived live rather than read from IFunctionCallback-full-detail-report.md on purpose: that
/// document is a point-in-time snapshot and drifts, and a mock authored against a function this
/// agent cannot call is a case that can never pass.
/// </summary>
public static class MockTargetCatalogue
{
    /// <summary>Function names only, sorted and de-duplicated. The wire shape the UI consumes.</summary>
    public static List<string> Names(Agent agent)
        => Describe(agent).Select(t => t.Name).ToList();

    /// <summary>
    /// Every callable function with whatever description and parameter shape the agent definition
    /// carries.
    ///
    /// MCP tools come back name-only: <see cref="McpFunction"/> has nothing but a Name, so a model
    /// authoring a mock for one is working from the name alone. That is a real gap, not an oversight
    /// here -- it is why an MCP mock's argsMatchJson is more likely to need a human fix than a
    /// plugin function's.
    /// </summary>
    public static List<MockTargetInfo> Describe(Agent agent)
    {
        var targets = new List<MockTargetInfo>();

        foreach (var fn in (agent.Functions ?? []).Concat(agent.SecondaryFunctions ?? []))
        {
            if (string.IsNullOrWhiteSpace(fn?.Name)) continue;
            targets.Add(new MockTargetInfo(fn!.Name, fn.Description, ParameterSummary(fn.Parameters)));
        }

        foreach (var fn in (agent.McpTools ?? []).SelectMany(t => t.Functions ?? []))
        {
            if (string.IsNullOrWhiteSpace(fn?.Name)) continue;
            targets.Add(new MockTargetInfo(fn!.Name, null, null));
        }

        // First entry wins on a duplicate name: a function declared both primary and secondary is
        // one function, and the primary declaration is the one with the fuller definition.
        return targets
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// A one-line rendering of a function's parameters, e.g. <c>work_order_id* (string), note</c>,
    /// with a star on the required ones.
    ///
    /// Flattened rather than passed as raw JSON schema: the whole catalogue goes into one prompt, and
    /// full schemas for thirty functions crowd out the agent instruction and the existing cases --
    /// which are what actually make an authored case realistic. Names and types are enough for the
    /// only thing the model writes with them, an argsMatchJson subset.
    /// </summary>
    private static string? ParameterSummary(FunctionParametersDef? parameters)
    {
        var properties = parameters?.Properties;
        if (properties == null) return null;

        JsonElement root;
        try
        {
            root = properties.RootElement;
        }
        catch (ObjectDisposedException)
        {
            // The agent definition is shared, and a JsonDocument someone else already disposed must
            // not take down the authoring request with it.
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object) return null;

        var required = parameters!.Required ?? [];
        var parts = new List<string>();

        foreach (var property in root.EnumerateObject())
        {
            var builder = new StringBuilder(property.Name);
            if (required.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) builder.Append('*');

            if (property.Value.ValueKind == JsonValueKind.Object
                && property.Value.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String)
            {
                builder.Append(" (").Append(type.GetString()).Append(')');
            }

            parts.Add(builder.ToString());
        }

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }
}

/// <summary>One callable function, as much as the agent definition knows about it.</summary>
/// <param name="Name">The function name a mock or a toolCalled assertion has to match exactly.</param>
/// <param name="Description">Null for MCP tools -- see <see cref="MockTargetCatalogue.Describe"/>.</param>
/// <param name="Parameters">One-line parameter summary, or null when the function takes none.</param>
public record MockTargetInfo(string Name, string? Description, string? Parameters);
