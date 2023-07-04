# botsharp-plugin-wechat

A channel plugin of BotSharp for Tencent WeChat

### How to run locally
```
git clone https://github.com/SciSharp/BotSharp
```

Check appsettings.json to import module
```
{
  "PluginLoader": {
    "Assemblies": [
      ...
      "BotSharp.Plugin.WeChat"
    ],
    "Plugins": [
      ...
      "WeChatPlugin"
    ]
  }
}
```

Update appsettings.json to set the corresponding KEY
```
{
   "WeChat": {
    "AgentId": "437bed34-1169-4833-95ce-c24b8b56154a",
    "Token": "#{Token}#",
    "EncodingAESKey": "#{EncodingAESKey}#",
    "WeixinAppId": "#{WeixinAppId}#",
    "WeixinAppSecret": "#{WeixinAppSecret}#"
  }
}
```

Update WeChat WebHook form https://mp.weixin.qq.com/ ,set as `https://{HOST}/WeChatAsync`

F5 run WebStarter

Access http://localhost:5500

If you are debugging and developing locally, you can use an intranet penetration tool to receive WeChat message push. It is recommended to use the Dev Tunnels of VS.
