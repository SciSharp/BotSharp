# Chatbot Platform Builder
## Powered by LLM

[![Discord](https://img.shields.io/discord/1106946823282761851?label=Discord)](https://discord.gg/qRVm82fKTS)
[![QQ群聊](https://img.shields.io/static/v1?label=QQ&message=群聊&color=brightgreen)](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=sN9VVMwbWjs5L0ATpizKKxOcZdEPMrp8&authKey=RLDw41bLTrEyEgZZi%2FzT4pYk%2BwmEFgFcrhs8ZbkiVY7a4JFckzJefaYNW6Lk4yPX&noverify=0&group_code=985366726)
[![Join the chat at https://gitter.im/publiclab/publiclab](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/sci-sharp/community) 
[![Apache 2.0](https://img.shields.io/hexpm/l/plug.svg)](https://raw.githubusercontent.com/Oceania2018/BotSharp/master/LICENSE) 
[![NuGet](https://img.shields.io/nuget/dt/BotSharp.Core.svg)](https://www.nuget.org/packages/BotSharp.Core) 
[![Build status](https://ci.appveyor.com/api/projects/status/qx2dx5ca5hjqodm5?svg=true)](https://ci.appveyor.com/project/Haiping-Chen/botsharp)
[![Documentation Status](https://readthedocs.org/projects/botsharp/badge/?version=latest)](https://botsharp.readthedocs.io/en/latest/?badge=latest)

*"Conversation as a platform (CaaP) is the future, so it's perfect that we're already offering the whole toolkits to our .NET developers using the BotSharp AI BOT Platform Builder to build a CaaP. It opens up as much learning power as possible for your own robots and precisely control every step of the AI processing pipeline."*
    
**BotSharp** is an open source machine learning framework for AI Bot platform builder. This project involves natural language understanding, computer vision and audio processing technologies, and aims to promote the development and application of intelligent robot assistants in information systems. Out-of-the-box machine learning algorithms allow ordinary programmers to develop artificial intelligence applications faster and easier. 

It's written in C# running on .Net Core that is full cross-platform framework, the plug-in and pipeline flow execution design is adopted to completely decouple the plug-ins. C# is a enterprise grade programming language which is widely used to code business logic in information management related system. More friendly to corporate developers. BotSharp adopts machine learning algrithm in C# directly. That will facilitate the feature of the typed language C#, and be more easier when refactoring code in system scope. 

Why we do this? Because we all know Python is not friendly programming language for enterprise developers, it's not only because it's low performance but also it's a type weak language, it will be a disaster if you use Python to build your bussiness system.

BotSharp is in accordance with components principle strictly, decouples every part that is needed in the platform builder. So you can choose different UI/UX, or pick up a different NLP Tagger, or select a more advanced algorithm to do NER task. They are all modulized based on unified interfaces.

### Some Features

* Built-in multi-agents and conversation management.
* Support multiple LLM platforms.
* Support export/ import agent from other bot platforms directly. 
* Support different open source UI [Chatbot UI](src\Plugins\BotSharp.Plugin.ChatbotUI\Chatbot-UI.md), [HuggingChat UI](src\Plugins\BotSharp.Plugin.HuggingFace\HuggingChat-UI.md).
* Integrate with popular message channels like Facebook Messenger, Slack and Telegram.

### Quick Started
1. Run backend service
```sh
 PS D:\> git clone https://github.com/dotnetcore/BotSharp
 PS D:\> cd BotSharp
 PS D:\BotSharp\> dotnet run -p .\src\WebStarter
```
2. Run UI project, reference to [Chatbot UI](src\Plugins\BotSharp.Plugin.ChatbotUI\Chatbot-UI.md).

### Extension Libraries

BotSharp uses component design, the kernel is kept to a minimum, and business functions are implemented by external components. The modular design also allows contributors to better participate.

* Chatbot UI connector. 
* A channel module of BotSharp for Facebook Messenger.
* A channel module of BotSharp for Tencent Wechat.
* A channel module of BotSharp for Telegram.

### Documents

Read the docs: https://botsharp.readthedocs.io

If you feel that this project is helpful to you, please Star the project, we would be very grateful.

Member project of [SciSharp STACK](https://github.com/SciSharp) which is the .NET based ecosystem of open-source software for mathematics, science, and engineering.

Scan QR code to join TIM group:

![SciSharp STACK](https://raw.githubusercontent.com/SciSharp/TensorFlow.NET/master/docs/TIM.jpg)
