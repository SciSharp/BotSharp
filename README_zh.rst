开源人工智能聊天机器人平台构建框架
===============================

.. image:: https://img.shields.io/badge/gitter-join%20chat-brightgreen.svg
    :target: `gitter`_
    :alt: gitter
    :align: left
    
.. image:: https://img.shields.io/hexpm/l/plug.svg   
    :target: `license`_
    :alt: Hex.pm
    :align: left

.. image:: https://img.shields.io/nuget/dt/EntityFrameworkCore.BootKit.svg
    :target: `botsharpnuget`_
    :alt: NuGet

本项目用于学习和交流人工智能聊天机器人平台构建的相关技术，本人对于任何用于生产环境而产生的风险和损失不负责任
**************************************************************************************************

*"Conversation as a platform (CaaP) is the future, so it's perfect that we're already offering the whole toolkits to our .NET developers using the BotSharp AI BOT Platform Builder to build a CaaP. It opens up as much learning power as possible for your own robots and precisely control every step of the AI processing pipeline."*
    
**BotSharp** is an open source machine learning framework for AI Bot platform builder. This project involves natural language understanding, computer vision and audio processing technologies, and aims to promote the development and application of intelligent robot assistants in information systems. Out-of-the-box machine learning algorithms allow ordinary programmers to develop artificial intelligence applications faster and easier. 

.. raw:: html

    <img src="https://raw.githubusercontent.com/Oceania2018/BotSharp/master/docs/static/logos/BotSharpEngine.jpg" width="100%">
    
It's witten  in C# running on .Net Core that is full cross-platform framework. C# is a enterprise grade programming language which is widely used to code business logic in information management related system. More friendly to corporate developers. BotSharp adopts machine learning algrithm in C# directly. That will facilitate the feature of the typed language C#, and be more easier when refactoring code in system scope. 

Why we do this? because we all know python is not friendly programming language for enterprise developers, it's not only because it's low performance but also it's a type weak language, it will be a disater if you use python to build your bussiness system.

BotSharp is in accordance with components princple strictly, decouples every part that needed in the platform builder. So you can choose different UI/UX, or pick up a different NLP Tagger, or select a more advanced algrithm to do NER task. They are all modulized based an unfied interfaces.

Some Features
-------------

* Integrated debugging is easier without relying on any other machine learning algorithm libraries.
* Built-in multi-Agents management, easy to build Bot as a Service platform.
* Context In/ Out with lifespan to make conversion flow be controllable.
* Use the natural language processing pipeline mechanism to work with extensions easily, and build your own unique robot processing flows. 
* Rewrote NLP algorithm from ground without historical issues.
* Support export/ import agent from other bot platforms directly. 
* Support different UI providers like `Rasa UI`_ and `Articulate UI`_.
* Support for multiple data request and response formats such as Rasa NLU and Dialogflow.
* Integrate with popular social platforms like Facebook Messenger, Slack and Telegram.
* Multi-core parallel computing optimization, High-Performance C# on GPUs in Hybridizer.

Quick Started
-------------
You can use docker compose to run BotSharp quickly, make sure you've got `Docker`_ installed.
::

 PS D:\> git clone https://github.com/Oceania2018/BotSharp
 PS D:\> cd BotSharp
 PS D:\BotSharp\> docker-compose -f dockerfiles/docker-compose-articulateui.yml up

Point your web browser at http://localhost:3000 and enjoy BotSharp with Articulate-UI.

Extension Libraries
-----------------
BotSharp uses component design, the kernel is kept to a minimum, and business functions are implemented by external components. The modular design also allows contributors to better participate.

* BotSharp platform emulator extension which is compatible with RASA NLU. `botsharp-rasa`_
* BotSharp platform emulator extension which is compatible with Google Dialogflow. `botsharp-dialogflow`_
* BotSharp platform emulator extension which is compatible with Articulate AI. `botsharp-articulate`_
* A channel module of BotSharp for Facebook Messenger. `botsharp-channel-fbmessenger`_
* A channel module of BotSharp for Tencent Weixin. `botsharp-channel-weixin`_

Documents
---------
Read the docs: https://botsharp.readthedocs.io

Feel free to message me on 

.. image:: https://img.shields.io/badge/gitter-join%20chat-brightgreen.svg
    :target: `gitter`_

If you feel that this project is helpful to you, please Star on the project, we will be very grateful.

Scan to join group in Wechat

.. raw:: html

    <img src="https://raw.githubusercontent.com/Oceania2018/BotSharp/master/docs/static/logos/WechatQRCode.png" width="150px">

.. _Docker: https://www.docker.com
.. _Rasa UI: https://github.com/paschmann/rasa-ui
.. _Articulate UI: https://spg.ai/projects/articulate
.. _gitter: https://gitter.im/botsharpcore/Lobby
.. _license: https://raw.githubusercontent.com/Oceania2018/BotSharp/master/LICENSE
.. _botsharpnuget: https://www.nuget.org/packages/BotSharp.Core
.. _botsharp-rasa: https://github.com/Oceania2018/botsharp-rasa
.. _botsharp-dialogflow: https://github.com/Oceania2018/botsharp-dialogflow
.. _botsharp-articulate: https://github.com/Oceania2018/botsharp-articulate
.. _botsharp-channel-fbmessenger: https://github.com/Oceania2018/botsharp-channel-fbmessenger
.. _botsharp-channel-weixin: https://github.com/Oceania2018/botsharp-channel-weixin

