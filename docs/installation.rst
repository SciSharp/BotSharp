Installation
============
BotSharp strictly follows the modular design principle and adopts a structure in which views and logic are separated. 
So you can choose the front-end Bot design and management interface. 
If you want to use the `Articulate UI`_ as a front end, you can use the articulateui-specific compose file to quickly experience BotSharp.

Docker Composer
^^^^^^^^^^^^^^^
You can use docker compose to run, make sure you've got `Docker`_ installed.

::

    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp

1. Integrate with `Botpress`_


2. Integrate with `Articulate UI`_

::

 PS D:\BotSharp\> docker-compose -f dockerfiles/docker-compose-articulateui.yml up

Point your web browser at http://localhost:3000 and enjoy Articulate-UI with BotSharp.
|ArticulateHomeScreenshot|

3. Integrate with `Rasa UI`_, you can use docker compose to run.

::

 PS D:\BotSharp\> docker-compose -f dockerfiles/docker-compose-rasaui.yml up

Point your web browser at http://localhost:5001 and enjoy Rasa-UI with BotSharp.

|RasaUIHomeScreenshot|

4. Integrate with `Rasa Talk`_


Building & Run locally
^^^^^^^^^^^^^^^^^^^^^^
If you are a .NET developer and want to develop extensions or fix bug for BotSharp, you would CTRL + F5 to run it locally in debug mode. 
Make sure the `Microsoft .NET Core`_ build environment and `Node.js`_ is installed. 
Building solution using dotnet CLI (preferred).

* Build NLU API
::

    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp
    PS D:\> dotnet build -v m -o ../bin -c ARTICULATE
    PS D:\> dotnet bin\BotSharp.WebHost.dll

If you don't have Redis installed, please update ArticulateAi.json:

::

"AgentStorage": "AgentStorageInRedis"

to 

::

"AgentStorage": "AgentStorageInMemory" 
  
* Build Chatbot Designer
::

    PS D:\> git clone https://github.com/Oceania2018/articulate-ui
    PS D:\> cd articulate-ui
    PS D:\> npm install
    PS D:\> npm start

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

|APIHomeScreenshot|


Install in NuGet
^^^^^^^^^^^^^^^^

::
 
 PM> Install-Package BotSharp.Core
 PM> Install-Package BotSharp.RestApi

Use BotSharp.NLP as a natural language processing toolkit alone.

::

 PM> Install-Package BotSharp.NLP

.. _Botpress: https://github.com/botpress/botpress
.. _Rasa UI: https://github.com/paschmann/rasa-ui
.. _Articulate UI: https://github.com/Oceania2018/articulate-ui
.. _Rasa Talk: https://github.com/jackdh/RasaTalk
.. _Microsoft .NET Core: https://www.microsoft.com/net/download
.. _Node.js: https://nodejs.org
.. _Docker: https://www.docker.com

.. |APIHomeScreenshot| image:: /static/screenshots/APIHome.png
.. |ArticulateHomeScreenshot| image:: /static/screenshots/ArticulateHome.png
.. |RasaUIHomeScreenshot| image:: /static/screenshots/RasaUIHome.png
