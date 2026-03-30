/*****************************************************************************
  Copyright 2024 Written by Jicheng Lu. All Rights Reserved.
 
  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at
 
      http://www.apache.org/licenses/LICENSE-2.0
 
  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
******************************************************************************/

using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Core.SideCar.Services;

public class BotSharpConversationSideCar : IConversationSideCar
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BotSharpConversationSideCar> _logger;

    private Stack<ConversationContext> _contextStack = new();
    private SideCarOptions? _sideCarOptions;

    private bool _enabled = false;
    private string _conversationId = string.Empty;

    public string Provider => "botsharp";
    public bool IsEnabled => _enabled;

    public BotSharpConversationSideCar(
        IServiceProvider services,
        ILogger<BotSharpConversationSideCar> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task AppendConversationDialogs(string conversationId, List<DialogElement> messages)
    {
        if (!IsValid(conversationId))
        {
            return;
        }

        var top = _contextStack.Peek();
        top.Dialogs.AddRange(messages);

        await Task.CompletedTask;
    }

    public async Task<List<DialogElement>> GetConversationDialogs(string conversationId, ConversationDialogFilter? filter = null)
    {
        if (!IsValid(conversationId))
        {
            return [];
        }

        var dialogs = _contextStack.Peek().Dialogs ?? [];
        if (filter?.Order == "desc")
        {
            dialogs = dialogs.OrderByDescending(x => x.MetaData?.CreatedTime).ToList();
        }

        return await Task.FromResult(dialogs);
    }

    public async Task UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
    {
        if (!IsValid(conversationId))
        {
            return;
        }

        var top = _contextStack.Peek().Breakpoints;
        top.Add(breakpoint);

        await Task.CompletedTask;
    }

    public async Task<ConversationBreakpoint?> GetConversationBreakpoint(string conversationId)
    {
        if (!IsValid(conversationId))
        {
            return null;
        }

        var top = _contextStack.Peek().Breakpoints;
        return await Task.FromResult(top.LastOrDefault());
    }

    public async Task UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (!IsValid(conversationId))
        {
            return;
        }

        var top = _contextStack.Peek();
        top.State = new ConversationState(states);

        await Task.CompletedTask;
    }

    public async Task<RoleDialogModel> SendMessage(
        string agentId,
        string text,
        PostbackMessageModel? postback = null,
        List<MessageState>? states = null,
        List<DialogElement>? dialogs = null,
        SideCarOptions? options = null)
    {
        _sideCarOptions = options;
        _logger.LogInformation($"Entering side car conversation...");

        BeforeExecute(dialogs);
        var response = await InnerExecute(agentId, text, postback, states);
        AfterExecute();

        _logger.LogInformation($"Exiting side car conversation...");
        return response;
    }

    private async Task<RoleDialogModel> InnerExecute(string agentId, string text,
        PostbackMessageModel? postback = null, List<MessageState>? states = null)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        _conversationId = conv.ConversationId;

        var inputMsg = new RoleDialogModel(AgentRole.User, text);
        routing.Context.SetMessageId(conv.ConversationId, inputMsg.MessageId);
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));

        var response = new RoleDialogModel(AgentRole.Assistant, string.Empty);
        await conv.SendMessage(agentId, inputMsg,
            replyMessage: postback,
            async msg =>
            {
                response.Content = !string.IsNullOrEmpty(msg.SecondaryContent) ? msg.SecondaryContent : msg.Content;
                response.FunctionName = msg.FunctionName;
                response.RichContent = msg.SecondaryRichContent ?? msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
            });

        return response;
    }

    private void BeforeExecute(List<DialogElement>? dialogs)
    {
        _enabled = true;
        var state = _services.GetRequiredService<IConversationStateService>();
        var routing = _services.GetRequiredService<IRoutingService>();

        var node = new ConversationContext
        {
            State = state.GetCurrentState(),
            Dialogs = dialogs ?? [],
            RoutingDialogs = routing.Context.GetDialogs(),
            Breakpoints = [],
            RecursiveCounter = routing.Context.GetRecursiveCounter(),
            RoutingStack = routing.Context.GetAgentStack()
        };
        _contextStack.Push(node);

        // Reset
        state.ResetCurrentState();
        routing.Context.ResetRecursiveCounter();
        routing.Context.ResetAgentStack();
        routing.Context.ResetDialogs();
        Utilities.ClearCache();
    }

    private void AfterExecute()
    {
        var routing = _services.GetRequiredService<IRoutingService>();
        var node = _contextStack.Pop();

        // Recover
        RestoreStates(node.State);
        routing.Context.SetRecursiveCounter(node.RecursiveCounter);
        routing.Context.SetAgentStack(node.RoutingStack);
        routing.Context.SetDialogs(node.RoutingDialogs);
        Utilities.ClearCache();
        _enabled = false;
    }

    private bool IsValid(string conversationId)
    {
        return !_contextStack.IsNullOrEmpty()
            && _conversationId == conversationId
            && !string.IsNullOrEmpty(conversationId)
            && !string.IsNullOrEmpty(_conversationId);
    }

    private void RestoreStates(ConversationState prevStates)
    {
        var preValues = prevStates.Values.ToList();
        var copy = JsonSerializer.Deserialize<List<StateKeyValue>>(JsonSerializer.Serialize(preValues));
        var innerStates = new ConversationState(copy ?? []);
        var state = _services.GetRequiredService<IConversationStateService>();

        if (_sideCarOptions?.IsInheritStates == true)
        {
            var hasIncludedStates = _sideCarOptions?.InheritStateKeys?.Any() == true;
            var hasExcludedStates = _sideCarOptions?.ExcludedStateKeys?.Any() == true;
            var curStates = state.GetCurrentState();

            foreach (var pair in curStates)
            {
                var endNode = pair.Value.Values.LastOrDefault();
                if (endNode == null) continue;

                if ((hasIncludedStates && !_sideCarOptions.InheritStateKeys.Contains(pair.Key))
                    || (hasExcludedStates && _sideCarOptions.ExcludedStateKeys.Contains(pair.Key)))
                {
                    continue;
                }

                if (innerStates.ContainsKey(pair.Key) && innerStates[pair.Key].Versioning)
                {
                    innerStates[pair.Key].Values.Add(endNode);
                }
                else
                {
                    innerStates[pair.Key] = new StateKeyValue
                    {
                        Key = pair.Key,
                        Versioning = pair.Value.Versioning,
                        Readonly = pair.Value.Readonly,
                        Values = [endNode]
                    };
                }
            }
        }

        AccumulateLlmStats(state, prevStates, innerStates);
        state.SetCurrentState(innerStates);
    }

    private void AccumulateLlmStats(IConversationStateService state, ConversationState prevState, ConversationState curState)
    {
        var dict = new Dictionary<string, Type>
        {
            { "prompt_total", typeof(int) },
            { "completion_total", typeof(int) },
            { "llm_total_cost", typeof(float) }
        };

        foreach (var pair in dict)
        {
            var preVal = prevState.GetValueOrDefault(pair.Key)?.Values?.LastOrDefault()?.Data;
            var curVal = state.GetState(pair.Key);

            object data = pair.Value switch
            {
                Type t when t == typeof(int) => ParseNumber<int>(preVal) + ParseNumber<int>(curVal),
                Type t when t == typeof(float) => ParseNumber<float>(preVal) + ParseNumber<float>(curVal),
                _ => default
            };

            var cur = curState.GetValueOrDefault(pair.Key);
            if (cur?.Values?.LastOrDefault() != null)
            {
                cur.Values.Last().Data = $"{data}";
            }
        }
    }

    private T ParseNumber<T>(string? data) where T : struct
    {
        if (string.IsNullOrEmpty(data))
        {
            return default;
        }

        return typeof(T) switch
        {
            Type t when t == typeof(int) => (T)(object)(int.TryParse(data, out var i) ? i : 0),
            Type t when t == typeof(float) => (T)(object)(float.TryParse(data, out var f) ? f : 0),
            _ => default
        };
    }
}