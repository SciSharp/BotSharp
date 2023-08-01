.. BotSharp documentation master file, created by
   sphinx-quickstart on Sun Aug 19 10:40:29 2018.
   You can adapt this file completely to your liking, but it should at least
   contain the root `toctree` directive.

The Open Source AI Bot Platform Builder
======================================================

.. image:: https://img.shields.io/discord/1106946823282761851?label=Discord
    :target: `discord`_

**Build the AI chatbot platform from scratch with .NET**

> The LLM powered Conversational Service framework

*"Conversation as a platform (CaaP) is the future, so it's perfect that we're already offering the whole toolkits to .NET developers using BotSharp the Bot Platform Builder to build a CaaP. It opens up as much learning power as possible for your robots and precisely control every step of the AI processing pipeline."*

**BotSharp** is an open source bot framework for AI Bot platform builders. This project involves natural language understanding, computer vision and audio processing technologies, and aims to promote the development and application of intelligent robot assistants in information systems. Out-of-the-box machine learning algorithms allow ordinary programmers to develop artificial intelligence applications faster and easier. 

It's witten in C# running on .NET which is a full cross-platform framework. C# is an enterprise-grade programming language which is widely used to code business logic in information management related system. More friendly to corporate developers. BotSharp adopts machine learning algrithm in C/C++ interfaces directly which skips the python interfaces. That will facilitate the feature of the typed language C#, and be easier when refactoring code in system scope. 

BotSharp is strictly in accordance with the components principle and decouples every part that is needed in the platform builder. So you can choose different UI/UX, or pick up a different NLP Tagger, or select a more advanced algorithm to do NER tasks. They are all modularized based on unified interfaces.

.. image:: static/logos/BotSharp.png
   :height: 64px

Some Features
-------------

* Built-in multi-Agents management, easy to build Bot as a Service platform.
* Integrate with multiple LLMs like ChatGPT and LLaMA.
* Using plug-in design, it is easy to expand functions. 
* Working with multiple Vector Stores for senmatic search.
* Supporting different UI providers like `Chatbot UI`_ and `HuggingChat UI`_.
* Integrated with popular social platforms like Facebook Messenger, Slack and Telegram.
* Providing REST APIs to work with your own UI.

Indices and tables
==================
The main documentation for the site is organized into the following sections:

* :ref:`Get Started <get-started>`
* :ref:`Integration Documentation <integration-docs>`
* :ref:`Architecture Documentation <architecture-docs>`
* :ref:`search`

.. _get-started:

.. toctree::
   :maxdepth: 3
   :caption: Get Started with BotSharp
   
   quick-start/overview
   quick-start/installation

.. _agent-docs:

.. toctree::
   :maxdepth: 3
   :caption: Agent & Conversation

   agent/account
   agent/conversation

.. _integration-docs:

.. toctree::
   :maxdepth: 3
   :caption: Channels Integration Documentation:

   integrations/facebook-messenger

.. _architecture-docs:

.. toctree::
   :maxdepth: 3
   :caption: Architecture Documentation:
   
   configuration/db
   configuration/platform

If you feel that this project is helpful to you, please Star us on the project, we will be very grateful.

.. _Chatbot UI: https://github.com/mckaywrigley/chatbot-ui
.. _HuggingChat UI: https://github.com/huggingface/chat-ui
.. _discord: https://discord.gg/qRVm82fKTS
