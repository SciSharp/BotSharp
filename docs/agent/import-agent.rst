Import Agent
============
Designed as a multi-platform framework, BotSharp allows developers to create their own Bot platforms and support multiple Bot platform services. It supports multiple Bot import, export and message reply formats such as Dialogflow and Rasa.
Support for importing and exporting between platforms.

**First, export agent from other chatbot platform.**

In general, the platform provides the ability to export to a compressed file. Different platform has different export method.

**Second, add meta.json to zip file.**

meta.json is used to tell BotSharp where the agent is exported from. It should looks like below:

.. code-block:: json

    {
        "Id": "YOURS",
        "Name": "YOURS",
        "Platform": "Dialogflow",
        "ClientAccessToken": "YOURS",
        "DeveloperAccessToken": "YOURS",
        "Integrations": []
    }

Extract zip file and add the meta.json to the zip file.    

1. Google Dialogflow
::

"Platform": "Dialogflow"

2. RASA
::

"Platform": "Rasa"

3. Microsoft LUIS

**Last, upload updated zip file.**

Upload zip file in REST API.

|RestoreAgentFromZipScreenshot|

.. |RestoreAgentFromZipScreenshot| image:: /static/screenshots/RestoreAgentFromZip.png