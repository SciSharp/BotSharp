The Open Source AI Bot Platform Builder for Enterprise
======================================================

*"Conversation as a platform (CaaP) is the future, so it's perfect that we're already offering the whole toolkits to our enterprise developers using the BotSharp Bot Platform Builder to build a CaaP. It opens up as much learning power as possible for your enterprise robots and precisely control every step of the AI processing pipeline."*

**BotSharp** is an open source machine learning framework for AI Bot platform builder. This project involves natural language understanding, computer vision and audio processing technologies, and aims to promote the development and application of intelligent robot assistants in enterprise information systems. Out-of-the-box machine learning algorithms allow ordinary programmers to develop artificial intelligence applications faster and easier. 

It's witten  in C# running on .Net Core that is full cross-platform framework. C# is a enterprise grade programming language which is widely used to code business logic in information management related system. More friendly to corporate developers. BotSharp adopts machine learning algrithm in C/C++ interfaces directly which skips the python interfaces. That will facilitate the feature of the typed language C#, and be more easier when refactoring code in system scope. 

Why we do this? because we all know python is not friendly programming language for enterprise developers, it's not only because it's low performance but also it's a type weak language, it will be a disater if you use python to build your bussiness system.

BotSharp is in accordance with components princple strictly, decouples every part that needed in the platform builder. So you can choose different UI/UX, or pick up a different NLP Tagger, or select a more advanced algrithm to do NER task. They are all modulized based an unfied interfaces.

Some Features
-------------

* Built-in multi-Agents management, easy to build Bot as a Service platform.
* Context In/ Out with lifespan to make conversion flow be controllable.
* Use the natural language processing pipeline mechanism and the popular NLP algorithm library to build your own unique robot processing flow.
* Support export/ import agent from other bot platforms directly. 
* Support different UI providers like `Rasa UI`_ and `Articulate UI`_.
* Support for multiple data request and response formats such as Rasa NLU and Dialogflow.
* Integrate with popular social platforms like Facebook Messenger, Slack and Telegram.

Documents
---------
https://botsharp.readthedocs.io/en/latest

https://github.com/Oceania2018/BotSharp/wiki

QUICK START
-----------
Building BotSharp
^^^^^^^^^^^^^^^^^
Make sure the `Microsoft .NET Core`_ build environment is installed. 
Building solution using dotnet CLI (preferred).

::

    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp
    PS D:\> dotnet build

Install in docker container
^^^^^^^^^^^^^^^^^^^^^^^^^^^
Make sure you've got `Docker`_ installed:

::
 
    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp
    
Build docker image:

::

 PS D:\BotSharp\> docker build -t botsharp .

Start a container:

::

 PS D:\BotSharp\> docker run -it -p 5000:5000 botsharp


We integrate Rasa UI with botsharp, you can use docker compose to run

docker-compose up

point your web browser at http://localhost:5001 and enjoy Rasa-UI with Botsharp


Install in NuGet
^^^^^^^^^^^^^^^^

::
 
 PM> Install-Package BotSharp.Core
 PM> Install-Package BotSharp.RestApi

Use BotSharp.NLP as a natural language processing toolkit alone.

::

 PM> Install-Package BotSharp.NLP



Tip Jar
-------
* Ethereum 0x2FdE97210cd14F6020C67BAFA61d4c227FdC268d

.. image:: https://raw.githubusercontent.com/Haiping-Chen/Etherscan.NetSDK/master/qr_code_eth.jpg
   :height: 160px

.. _Rasa UI: https://github.com/paschmann/rasa-ui
.. _Articulate UI: https://spg.ai/projects/articulate
.. _Microsoft .NET Core: https://www.microsoft.com/net/download
.. _Docker: https://www.docker.com
