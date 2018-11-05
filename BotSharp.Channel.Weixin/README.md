# botsharp-channel-weixin
A channel module of BotSharp for Tencent Weixin

### How to install through NuGet

```
PM> Install-Package BotSharp.Channel.Weixin
```

### How to run locally
```
git clone https://github.com/dotnetcore/BotSharp
```

Check app.json to use DialogflowAi
```
{
  "version": "0.1.0",
  "assemblies": "BotSharp.Core",

  "platformModuleName": "DialogflowAi"
}
```

Update channels.weixin.json to set the corresponding KEY
```
{
  "weixinChannel": {
    "token": "botsharp",
    "encodingAESKey": "",
    "appId": "",
    "agentId": "60bee6f9-ba58-4fe8-8b95-94af69d6fd41"
  }
}
```

F5 run BotSharp.WebHost

Access http://localhost:3112

Import demo (Spotify.zip) agent located at App_Data

Train agent (id: 60bee6f9-ba58-4fe8-8b95-94af69d6fd41)

Or refer [BotSharp docs](https://botsharp.readthedocs.io) to design your new chatbot.

Setup Wechat webhood from https://mp.weixin.qq.com/.
