# botsharp-dialogflow
BotSharp platform extension which is compatible with Google Dialogflow.

### How to install
```
PM> Install-Package BotSharp.Platform.Dialogflow
```

### How to use
```
private IPlatformBuilder<AgentModel> nluPlatform = null;
public CustomMessageHandler(IPlatformBuilder<AgentModel> nluPlatform)
{
    this.nluPlatform = nluPlatform;
}

// send text to BotSharp platform emulator
var aIResponse = nluPlatform.TextRequest<AIResponseResult>(new AiRequest
{
    Text = requestMessage.Content,
    AgentId = "REPLACE_AGENT_ID", // replace to your chabot's id
    SessionId = requestMessage.FromUserName
});

```
### How to run locally
```
git clone https://github.com/dotnetcore/BotSharp
```