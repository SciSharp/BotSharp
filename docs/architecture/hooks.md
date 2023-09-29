# Hooks

`Hook` is a place in `BotSharp` that allows you to tap in to a module to either provide different behavior or to react.

## Agent Hook
`IAgentHook`
```csharp
bool OnAgentLoading(ref string id);
bool OnInstructionLoaded(string template, Dictionary<string, object> dict);
bool OnFunctionsLoaded(List<FunctionDef> functions);
bool OnSamplesLoaded(ref string samples);
Agent OnAgentLoaded();
```
More information about agent hook please go to [Agent Hook](../agent/hook.md).

## Conversation Hook
`IConversationHook`
```csharp
Task OnDialogsLoaded(List<RoleDialogModel> dialogs);
Task BeforeCompletion();
Task OnFunctionExecuting(RoleDialogModel message);
Task OnFunctionExecuted(RoleDialogModel message);
Task AfterCompletion(RoleDialogModel message);
```
More information about conversation hook please go to [Conversation Hook](../conversation/hook.md).

### Conversation State Hook
`IConversationHook`
```csharp
Task OnStateLoaded(ConversationState state);
Task OnStateChanged(string name, string preValue, string currentValue);
```