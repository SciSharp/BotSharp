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

using BotSharp.Abstraction.SideCar.Models;
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

    public BotSharpConversationSideCar(
        IServiceProvider services,
        ILogger<BotSharpConversationSideCar> logger)
    {
        _services = services;
        _logger = logger;
    }

    public bool IsEnabled()
    {
        return _enabled;
    }

    public void AppendConversationDialogs(string conversationId, List<DialogElement> messages)
    {
        if (!IsValid(conversationId))
        {
            return;
        }

        var top = _contextStack.Peek();
        top.Dialogs.AddRange(messages);
    }

    public List<DialogElement> GetConversationDialogs(string conversationId)
    {
        if (!IsValid(conversationId))
        {
            return new List<DialogElement>();
        }

        return _contextStack.Peek().Dialogs;
    }

    public void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
    {
        if (!IsValid(conversationId))
        {
            return;
        }

        var top = _contextStack.Peek().Breakpoints;
        top.Add(breakpoint);
    }

    public ConversationBreakpoint? GetConversationBreakpoint(string conversationId)
    {
        if (!IsValid(conversationId))
        {
            return null;
        }

        var top = _contextStack.Peek().Breakpoints;
        return top.LastOrDefault();
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (!IsValid(conversationId))
        {
            return;
        }

        var top = _contextStack.Peek();
        top.State = new ConversationState(states);
    }

    public async Task<RoleDialogModel> SendMessage(string agentId, string text,
        PostbackMessageModel? postback = null,
        List<MessageState>? states = null,
        List<DialogElement>? dialogs = null,
        SideCarOptions? options = null)
    {
        _sideCarOptions = options;

        BeforeExecute(dialogs);
        var response = await InnerExecute(agentId, text, postback, states);
        AfterExecute();
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
        var state = _services.GetRequiredService<IConversationStateService>();
        var routing = _services.GetRequiredService<IRoutingService>();

        var node = _contextStack.Pop();

        // Recover
        state.SetCurrentState(node.State);
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
        var innerStates = prevStates;
        var state = _services.GetRequiredService<IConversationStateService>();

        if (_sideCarOptions?.IsInheritStates == true)
        {
            var curStates = state.GetCurrentState();
            foreach (var pair in curStates)
            {
                var endNode = pair.Value.Values.LastOrDefault();
                if (endNode == null) continue;

                if (_sideCarOptions?.InheritStateKeys?.Any() == true
                    && !_sideCarOptions.InheritStateKeys.Contains(pair.Key))
                {
                    continue;
                }

                if (innerStates.ContainsKey(pair.Key))
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

        state.SetCurrentState(innerStates);
    }
}