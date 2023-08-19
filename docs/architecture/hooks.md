# Hooks

`Hook` is a place in `BotSharp` that allows you to tap in to a module to either provide different behavior or to react.

## Agent Hook
`IAgentHook`
```csharp
bool OnAgentLoading(ref string id);
bool OnInstructionLoaded(ref string instruction);
bool OnFunctionsLoaded(ref string functions);
bool OnSamplesLoaded(ref string samples);
Agent OnAgentLoaded();
```

## Conversation Hook
`IConversationHook`
```csharp
Task OnDialogsLoaded(List<RoleDialogModel> dialogs);
Task BeforeCompletion();
Task OnFunctionExecuting(RoleDialogModel message);
Task OnFunctionExecuted(RoleDialogModel message);
Task AfterCompletion(RoleDialogModel message);
```


### Conversation State Hook
`IConversationHook`
```csharp
Task OnStateLoaded(ConversationState state);
Task OnStateChanged(string name, string preValue, string currentValue);
```