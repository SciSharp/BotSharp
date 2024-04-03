using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Routing.Models;
using System.Drawing;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    /// <summary>
    /// If the target agent needs some required fields but the
    /// </summary>
    /// <returns></returns>
    public bool HasMissingRequiredField(RoleDialogModel message, out string agentId)
    {
        var args = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs);
        var routing = _services.GetRequiredService<IRoutingService>();

        var routingRules = routing.GetRulesByAgentName(args.AgentName);

        if (routingRules == null || !routingRules.Any())
        {
            agentId = message.CurrentAgentId;
            return false;
        }

        agentId = routingRules.First().AgentId;
        // Add routed agent
        message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, "route_to", agentId);

        // Check required fields
        var root = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
        var missingFields = new List<string>();
        foreach (var field in routingRules.Where(x => x.Required).Select(x => x.Field))
        {
            if (!root.EnumerateObject().Any(x => x.Name == field))
            {
                missingFields.Add(field);
            }
            else if (root.EnumerateObject().Any(x => x.Name == field) &&
                string.IsNullOrEmpty(root.EnumerateObject().FirstOrDefault(x => x.Name == field).Value.ToString()))
            {
                missingFields.Add(field);
            }
        }

        // Check if states contains the field according conversation context.
        var states = _services.GetRequiredService<IConversationStateService>();
        foreach (var field in missingFields.ToList())
        {
            if (!string.IsNullOrEmpty(states.GetState(field)))
            {
                var value = states.GetState(field);

                // Check if the value is correct data type
                var rule = routingRules.First(x => x.Field == field);
                if (rule.FieldType == "number")
                {
                    if (!long.TryParse(value, out var longValue))
                    {
                        states.SetState(field, "", isNeedVersion: true, source: StateSource.Application);
                        continue;
                    }
                }
                message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, field, value);
                missingFields.Remove(field);
            }
        }

        if (missingFields.Any())
        {
            // Add field to args
            message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, "missing_fields", missingFields);
            message.Content = $"missing some information: {string.Join(", ", missingFields)}";

            // Handle redirect
            var routingRule = routingRules.FirstOrDefault(x => missingFields.Contains(x.Field));
            if (!string.IsNullOrEmpty(routingRule.RedirectTo))
            {
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var record = db.GetAgent(routingRule.RedirectTo);

                // Add redirected agent
                message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, "redirect_to", record.Name);
                agentId = routingRule.RedirectTo;
                var logger = _services.GetRequiredService<ILogger<RouteToAgentFn>>();
#if DEBUG
                Console.WriteLine($"*** Routing redirect to {record.Name.ToUpper()} ***", Color.Yellow);
#else
                logger.LogInformation($"*** Routing redirect to {record.Name.ToUpper()} ***");
#endif
            }
            else
            {
                // back to router
                agentId = message.CurrentAgentId;
            }
        }

        return missingFields.Any();
    }

    private string AppendPropertyToArgs(string args, string key, string value)
    {
        return args.Substring(0, args.Length - 1) + $", \"{key}\": \"{value}\"" + "}";
    }

    private string AppendPropertyToArgs(string args, string key, IEnumerable<string> values)
    {
        string fields = string.Join(",", values.Select(x => $"\"{x}\""));
        return args.Substring(0, args.Length - 1) + $", \"{key}\": [{fields}]" + "}";
    }
}
