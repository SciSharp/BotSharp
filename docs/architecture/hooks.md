# Hooks

`Hook` is a place in `BotSharp` that allows you to tap in to a module to either provide different behavior or to react.

## Agent Hook
`IAgentHook`
```csharp
// Triggered when agent is loading.
bool OnAgentLoading(ref string id);
bool OnInstructionLoaded(string template, Dictionary<string, object> dict);
bool OnFunctionsLoaded(List<FunctionDef> functions);
bool OnSamplesLoaded(ref string samples);

// Triggered when agent is loaded completely.
void OnAgentLoaded(Agent agent);
```
More information about agent hook please go to [Agent Hook](../agent/hook.md).

## Conversation Hook
`IConversationHook`
```csharp
// Triggered once for every new conversation.
Task OnConversationInitialized(Conversation conversation);
Task OnDialogsLoaded(List<RoleDialogModel> dialogs);
Task OnMessageReceived(RoleDialogModel message);

// Triggered before LLM calls function.
Task OnFunctionExecuting(RoleDialogModel message);

// Triggered when the function calling completed.
Task OnFunctionExecuted(RoleDialogModel message);
Task OnResponseGenerated(RoleDialogModel message);

// LLM detected the current task is completed.
Task OnCurrentTaskEnding(RoleDialogModel message);

// LLM detected the user's intention to end the conversation
Task OnConversationEnding(RoleDialogModel message);

// LLM can't handle user's request or user requests human being to involve.
Task OnHumanInterventionNeeded(RoleDialogModel message);
```
More information about conversation hook please go to [Conversation Hook](../conversation/hook.md).

### Conversation State Hook
`IConversationHook`
```csharp
Task OnStateLoaded(ConversationState state);
Task OnStateChanged(string name, string preValue, string currentValue);
```

### Content Generating Hook
`IContentGeneratingHook`

Model content generating hook, it can be used for logging, metrics and tracing.
```csharp
// Before content generating.
Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations);

// After content generated.
Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats);
```