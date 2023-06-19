Installation
============
BotSharp strictly follows the modular design principle and adopts a structure in which views and logic are separated. 
So you can choose the front-end Bot design and management interface. 


Building & Run locally
^^^^^^^^^^^^^^^^^^^^^^
If you are a .NET developer and want to develop extensions or fix bug for BotSharp, you would hit F5 to run it locally in debug mode. 
Make sure the `Microsoft .NET SDK`_ 6.0+ build environment and `Node.js`_ is installed. 
Building solution using dotnet CLI (preferred).

* Build BotSharp backend API
::

    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp
    PS D:\> dotnet build

* Update `appsettings.json`, BotSharp can work with serveral LLM providers. Below config is tasking Azure OpenAI as the LLM backend
::

    "AzureOpenAi": {
        "ApiKey": "",
        "Endpoint": "https://.openai.azure.com/",
        "DeploymentName": ""
    }

* Run backend web project
::

    PS D:\> dotnet run --project src/WebStarter

|BackendServiceHomeScreenshot|

* Open REST API in browser http://localhost:5500/swagger

|APIHomeScreenshot|

* Launch a chatbot UI
If you want to use the `Chatbot UI`_ as a front end.
::

    PS D:\> git clone https://github.com/mckaywrigley/chatbot-ui
    PS D:\> cd chatbot-ui
    PS D:\> cd npm i
    PS D:\> cd npm run dev

Update API url in `.env.local` to your localhost BotSharp backend service.
::
    OPENAI_API_HOST=http://localhost:5500


* Point your web browser at http://localhost:3000 and enjoy Chatbot with BotSharp.
|ChatbotUIHomeScreenshot|


Building docker image
^^^^^^^^^^^^^^^^^^^^^^^^^^^

If you just want to run BotSharp as a backend NLU engine, you can run it standalone in docker.

::
 
    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp
    
Build docker image:

::

 PS D:\BotSharp\> docker build -f dockerfiles/DIALOGFLOW.Dockerfile -t botsharp .

Start a container:

::

 PS D:\BotSharp\> docker run --name botsharp -it -p 5000:5000 botsharp

 
Access restful APIs: http://localhost:5000 if you are using RASA response format.

Get a bash shell if you want to update config in the container.

::

 PS D:\BotSharp\> docker exec -it botsharp /bin/bash


Install in NuGet
^^^^^^^^^^^^^^^^

::
 
 PM> Install-Package BotSharp.Core

Use BotSharp.NLP as a natural language processing toolkit alone.

::

 PM> Install-Package BotSharp.NLP

.. _Chatbot UI: https://github.com/mckaywrigley/chatbot-ui
.. _Microsoft .NET SDK: https://www.microsoft.com/net/download
.. _Node.js: https://nodejs.org
.. _Docker: https://www.docker.com

.. |BackendServiceHomeScreenshot| image:: /static/screenshots/BackendServiceHomeScreenshot.png
.. |APIHomeScreenshot| image:: /static/screenshots/APIHome.png
.. |ChatbotUIHomeScreenshot| image:: /static/screenshots/ChatbotUIHome.png
