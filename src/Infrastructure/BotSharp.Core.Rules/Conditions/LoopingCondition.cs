using System.Text.Json;

namespace BotSharp.Core.Rules.Conditions;

/// <summary>
/// A general loop condition node that iterates over a list of items in context parameters.
/// 
/// Expected parameters:
/// - "list_items": A JSON array of items to iterate over (e.g. ["a","b","c"] or [1,2,3] or [{...},{...}]).
/// - "iterate_index": The current iteration index (auto-managed, starts at 0).
/// - "iterate_current_item": The current item being processed (auto-set each iteration).
/// 
/// Flow:
///   Action Node → LoopCondition → (true) → back to Action Node
///                                → (false) → resets list_items, iterate_index, iterate_current_item and continues
/// </summary>
public sealed class LoopingCondition : IRuleCondition
{
    private const string PARAM_LIST_ITEMS = "list_items";
    private const string PARAM_LIST_ITEMS_KEY = "list_items_key";
    private const string PARAM_ITERATE_INDEX = "iterate_index";
    private const string PARAM_ITERATE_ITEM_KEY = "iterate_item_key";
    private const string PARAM_ITERATE_NEXT_ITEM = "iterate_next_item";

    private readonly ILogger<LoopingCondition> _logger;

    public LoopingCondition(ILogger<LoopingCondition> logger)
    {
        _logger = logger;
    }

    public string Name => "looping";

    public async Task<RuleNodeResult> EvaluateAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context)
    {
        try
        {
            context.Parameters ??= [];

            var listItemsRaw = string.Empty;
            var listItemsKey = context.Parameters.GetValueOrDefault(PARAM_LIST_ITEMS_KEY, string.Empty);
            if (!string.IsNullOrWhiteSpace(listItemsKey))
            {
                listItemsRaw = context.Parameters.GetValueOrDefault(listItemsKey, string.Empty);
            }
            else
            {
                listItemsRaw = context.Parameters.GetValueOrDefault(PARAM_LIST_ITEMS, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(listItemsRaw))
            {
                _logger.LogInformation("Loop condition: list items are empty, loop completed (agent {AgentId}).", agent.Id);
                CleanLoopState(context);
                return new RuleNodeResult
                {
                    Success = false,
                    Response = "Loop completed: list_items is empty."
                };
            }

            // Deserialize list_items as a JSON array of any type
            var items = JsonSerializer.Deserialize<JsonElement[]>(listItemsRaw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (items.IsNullOrEmpty())
            {
                _logger.LogInformation("Loop condition: no items to iterate, loop completed (agent {AgentId}).", agent.Id);
                CleanLoopState(context);
                return new RuleNodeResult
                {
                    Success = false,
                    Response = "Loop completed: no items in list."
                };
            }

            // If iterate_index is not yet set, this is the first visit after the action node
            // already handled item[0], so start from index 1.
            var indexStr = context.Parameters.GetValueOrDefault(PARAM_ITERATE_INDEX);
            int currentIndex;
            if (string.IsNullOrEmpty(indexStr))
            {
                currentIndex = 0;
            }
            else if (!int.TryParse(indexStr, out currentIndex))
            {
                currentIndex = 0;
            }

            var nextIndex = currentIndex + 1;
            if (currentIndex >= items!.Length || nextIndex >= items!.Length)
            {
                _logger.LogInformation("Loop condition: iteration complete ({Index}/{Total}) (agent {AgentId}).",
                    currentIndex, items.Length, agent.Id);
                CleanLoopState(context);
                return new RuleNodeResult
                {
                    Success = false,
                    Response = $"Loop completed: iterated over all {items.Length} items."
                };
            }

            // Set next item and advance index
            var nextElement = items[nextIndex];
            var nextItem = nextElement.ConvertToString();
            context.Parameters[PARAM_ITERATE_NEXT_ITEM] = nextItem;
            context.Parameters[PARAM_ITERATE_INDEX] = nextIndex.ToString();

            var data = new Dictionary<string, string>
            {
                [PARAM_ITERATE_NEXT_ITEM] = nextItem,
                [PARAM_ITERATE_INDEX] = nextIndex.ToString()
            };


            var itemKey = context.Parameters.GetValueOrDefault(PARAM_ITERATE_ITEM_KEY);
            if (!string.IsNullOrEmpty(itemKey)
                && nextElement.ValueKind == JsonValueKind.Object
                && nextElement.TryGetProperty(itemKey, out var fieldValue))
            {
                var fieldStr = fieldValue.ToString();
                context.Parameters[itemKey] = fieldStr;
                data[itemKey] = fieldStr;
            }

            _logger.LogInformation("Loop condition: processing item {Index}/{Total} = '{Item}' (agent {AgentId}).",
                nextItem, items.Length, nextElement, agent.Id);

            return new RuleNodeResult
            {
                Success = true,
                Response = $"Loop iteration {nextIndex}/{items.Length}: next item = {nextItem}",
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating loop condition for agent {AgentId}", agent.Id);
            CleanLoopState(context);
            return new RuleNodeResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private void CleanLoopState(RuleFlowContext context)
    {
        var itemKey = context.Parameters?.GetValueOrDefault(PARAM_ITERATE_ITEM_KEY);

        context.Parameters?.Remove(PARAM_LIST_ITEMS);
        context.Parameters?.Remove(PARAM_ITERATE_INDEX);
        context.Parameters?.Remove(PARAM_ITERATE_ITEM_KEY);
        context.Parameters?.Remove(PARAM_ITERATE_NEXT_ITEM);
        context.Parameters?.Remove(PARAM_LIST_ITEMS_KEY);

        if (!string.IsNullOrEmpty(itemKey))
        {
            context.Parameters?.Remove(itemKey);
        }
    }
}
