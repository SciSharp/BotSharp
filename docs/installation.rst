Installation
============
BotSharp strictly follows the modular design principle and adopts a structure in which views and logic are separated. 
So you can choose the front-end Bot design and management interface. 
If you want to use the `RASA UI`_ as a front end, you can use the rasaui-specific compose file to quickly experience BotSharp.

Docker Composer
^^^^^^^^^^^^^^^
You can use docker compose to run, make sure you've got `Docker`_ installed.

::

    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp
    
1. Integrate with `Articulate UI`_

::

 PS D:\BotSharp\> docker-compose -f docker-compose-articulateui.yml up

Point your web browser at http://localhost:3000 and enjoy Articulate-UI with BotSharp.

2. Integrate with `Rasa UI`_, you can use docker compose to run.

::

 PS D:\BotSharp\> docker-compose -f docker-compose-rasaui.yml up

Point your web browser at http://localhost:5001 and enjoy Rasa-UI with BotSharp.

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

::
 
    PS D:\> git clone https://github.com/Oceania2018/BotSharp
    PS D:\> cd BotSharp
    
Build docker image:

::

 PS D:\BotSharp\> docker build -t botsharp .

Start a container:

::

 PS D:\BotSharp\> docker run -it -p 5000:5000 botsharp

 



Install in NuGet
^^^^^^^^^^^^^^^^

::
 
 PM> Install-Package BotSharp.Core
 PM> Install-Package BotSharp.RestApi

Use BotSharp.NLP as a natural language processing toolkit alone.

::

 PM> Install-Package BotSharp.NLP


.. _Rasa UI: https://github.com/paschmann/rasa-ui
.. _Articulate UI: https://spg.ai/projects/articulate
.. _Microsoft .NET Core: https://www.microsoft.com/net/download
.. _Docker: https://www.docker.com