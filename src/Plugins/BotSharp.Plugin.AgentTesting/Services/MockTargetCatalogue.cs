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
    /// <summary>The prefix a utility item's function name must carry to be loaded as a function.</summary>
    /// <remarks>Same constant, same ordinal comparison, as BasicAgentHook.UTIL_PREFIX.</remarks>
    private const string UtilityPrefix = "util-";

    /// <summary>Function names only, sorted and de-duplicated. The wire shape the UI consumes.</summary>
    public static List<string> Names(Agent agent, Agent? utilityAssistant = null)
        => Describe(agent, utilityAssistant).Select(t => t.Name).ToList();

    /// <summary>
    /// Every callable function with whatever description and parameter shape the agent definition
    /// carries.
    ///
    /// MCP tools come back name-only: <see cref="McpFunction"/> has nothing but a Name, so a model
    /// authoring a mock for one is working from the name alone. That is a real gap, not an oversight
    /// here -- it is why an MCP mock's argsMatchJson is more likely to need a human fix than a
    /// plugin function's.
    /// </summary>
    /// <param name="agent">The agent whose callable surface is wanted.</param>
    /// <param name="utilityAssistant">
    /// The UtilityAssistant agent, which is where a utility item's real FunctionDef lives -- an agent
    /// only names the utilities it turns on. Pass null and utilities still appear, name-only.
    /// </param>
    public static List<MockTargetInfo> Describe(Agent agent, Agent? utilityAssistant = null)
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

        // Utilities are the third way an agent gets a function, and the one that does not show up
        // anywhere in Functions/SecondaryFunctions on a stored agent: BasicAgentHook.OnAgentUtilityLoaded
        // expands them into SecondaryFunctions at conversation time, and IAgentService.GetAgent -- what
        // every caller here holds -- is a plain repository read that never runs that hook. Left out, an
        // agent whose whole toolset is utilities (Lessen Work Order Summary, Property Summary) reports
        // no callable functions at all, and a case authored against it blocks its own tools on the
        // first run.
        //
        // The filter mirrors that hook: a disabled utility is off, and only a `util-` prefixed name is
        // ever loaded as a function. What is deliberately NOT mirrored is VisibilityExpression, which
        // needs the conversation's render data and cannot be evaluated against a stored agent -- a
        // conditionally-visible utility is offered here and may turn out not to load at run time.
        foreach (var utility in agent.Utilities ?? [])
        {
            if (utility == null || utility.Disabled) continue;

            foreach (var item in utility.Items ?? [])
            {
                var name = item?.FunctionName;
                if (string.IsNullOrWhiteSpace(name) || !name.StartsWith(UtilityPrefix, StringComparison.Ordinal)) continue;

                // The definition the agent will actually be given, not the agent's own declaration --
                // the description and parameter list live on UtilityAssistant.
                var definition = utilityAssistant?.Functions?
                    .FirstOrDefault(f => string.Equals(f?.Name, name, StringComparison.OrdinalIgnoreCase));

                targets.Add(new MockTargetInfo(
                    name!,
                    definition?.Description ?? item!.Description,
                    ParameterSummary(definition?.Parameters)));
            }
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
