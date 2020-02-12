# The Open Source AI Chatbot Platform Builder

[![Join the chat at https://gitter.im/publiclab/publiclab](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/sci-sharp/community) 
[![Apache 2.0](https://img.shields.io/hexpm/l/plug.svg)](https://raw.githubusercontent.com/Oceania2018/BotSharp/master/LICENSE) 
[![NuGet](https://img.shields.io/nuget/dt/BotSharp.Core.svg)](https://www.nuget.org/packages/BotSharp.Core) 
[![Build status](https://ci.appveyor.com/api/projects/status/qx2dx5ca5hjqodm5?svg=true)](https://ci.appveyor.com/project/Haiping-Chen/botsharp)
[![Documentation Status](https://readthedocs.org/projects/botsharp/badge/?version=latest)](https://botsharp.readthedocs.io/en/latest/?badge=latest)

*"Conversation as a platform (CaaP) is the future, so it's perfect that we're already offering the whole toolkits to our .NET developers using the BotSharp AI BOT Platform Builder to build a CaaP. It opens up as much learning power as possible for your own robots and precisely control every step of the AI processing pipeline."*
    
**BotSharp** is an open source machine learning framework for AI Bot platform builder. This project involves natural language understanding, computer vision and audio processing technologies, and aims to promote the development and application of intelligent robot assistants in information systems. Out-of-the-box machine learning algorithms allow ordinary programmers to develop artificial intelligence applications faster and easier. 

<img src="/docs/static/screenshots/BotSharp_arch.png" width="100%">

It's written in C# running on .Net Core that is full cross-platform framework. C# is a enterprise grade programming language which is widely used to code business logic in information management related system. More friendly to corporate developers. BotSharp adopts machine learning algrithm in C# directly. That will facilitate the feature of the typed language C#, and be more easier when refactoring code in system scope. 

Why we do this? Because we all know Python is not friendly programming language for enterprise developers, it's not only because it's low performance but also it's a type weak language, it will be a disaster if you use Python to build your bussiness system.

BotSharp is in accordance with components principle strictly, decouples every part that is needed in the platform builder. So you can choose different UI/UX, or pick up a different NLP Tagger, or select a more advanced algorithm to do NER task. They are all modulized based on unified interfaces.

### Some Features

* Integrated debugging is easier without relying on any other machine learning algorithm libraries.
* Built-in multi-Agents management, easy to build Bot as a Service platform.
* Context In/ Out with lifespan to make conversion flow be controllable.
* Use the natural language processing pipeline mechanism to work with extensions easily, and build your own unique robot processing flows. 
* Rewrote NLP algorithm from ground without historical issues.
* Support export/ import agent from other bot platforms directly. 
* Support different UI providers like `Rasa UI` and `Articulate UI`.
* Support for multiple data request and response formats such as Rasa NLU and Dialogflow.
* Integrate with popular social platforms like Facebook Messenger, Slack and Telegram.
* Multi-core parallel computing optimization, High-Performance C# on GPUs in Hybridizer.

### Quick Started

*  Make sure that you have downloaded the related components.
*  See the file "BotSharp\BotSharp.WebHost\Settings\app.json",change the path to your own project's path.
*  Select dialogflow or articulate to make it work.

You can use docker compose to run BotSharp quickly, make sure you've got `Docker`_ installed.

```sh
 PS D:\> git clone https://github.com/dotnetcore/BotSharp
 PS D:\> cd BotSharp
 PS D:\BotSharp\> docker-compose -f dockerfiles/docker-compose-core.yml up
```

Point your web browser at http://localhost:3112 and enjoy BotSharp Core.

### Extension Libraries

BotSharp uses component design, the kernel is kept to a minimum, and business functions are implemented by external components. The modular design also allows contributors to better participate.

* BotSharp platform emulator extension which is compatible with RASA NLU. 
* BotSharp platform emulator extension which is compatible with Google Dialogflow.
* BotSharp platform emulator extension which is compatible with Articulate AI.
* BotSharp platform emulator extension which is compatible with RasaTalk.
* A channel module of BotSharp for Facebook Messenger.
* A channel module of BotSharp for Tencent Wechat.
* A channel module of BotSharp for Telegram.
* Articulate UI customized for BotSharp NLU. 

### Documents

Read the docs: https://botsharp.readthedocs.io

If you feel that this project is helpful to you, please Star the project, we would be very grateful.

Member project of [SciSharp STACK](https://github.com/SciSharp) which is the .NET based ecosystem of open-source software for mathematics, science, and engineering.

Scan QR code to join TIM group:

![SciSharp STACK](https://raw.githubusercontent.com/SciSharp/TensorFlow.NET/master/docs/TIM.jpg)
