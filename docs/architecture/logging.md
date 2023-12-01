# Logging

## Setting
To initialize the logging feature, set up the following flags in `Conversation`. Each flag can display or record specific content during conversation.

* `ShowVerboseLog`: print conversation details or prompt in console.
* `EnableLlmCompletionLog`: log LLM completion results, e.g., real-time prompt sent to LLM and response generated from LLm.
* `EnableExecutionLog`: log details after events, e.g., receiving message, executing function, generating response, etc.


```json
"Conversation": {
    "ShowVerboseLog": false,
    "EnableLlmCompletionLog": false,
    "EnableExecutionLog": true
}
```

### Usage
To enable the logging functionality, add the following line of code in `Program.cs`.

```csharp
builder.Services.AddBotSharpLogger(builder.Configuration);
```
