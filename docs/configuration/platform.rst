Platform Settings
=================

* app.json

::

{
  "Assemblies": "BotSharp.Core",
  "BotPlatform": "BotSharpAi",
  "Version": "0.1.0",

  "MachineLearning": {
    "dataDir": "D:\\Projects\\BotSharp\\Data"
  }
}


* BotSharpAi.json

::

{
  "BotSharpAi": {
    "Lang": "en",

    "Provider": "BotSharpProvider",
    "BotSharpProvider": {
    },

    "Pipe": "BotSharpTokenizer, BotSharpTagger, BotSharpCRFNer, BotSharpIntentClassifier",

    "BotSharpTokenizer": {
      "tokenizer": "TreebankTokenizer"
    },

    "BotSharpIntentClassifier": {
      "classifer": "SVMClassifier"
    },

    "BotSharpTagger": {
      "tagger": "NGramTagger"
    },

    "BotSharpCRFNer": {
      "template": "|App_Data|CRFLite/template.en"
    },

    "WitAiEntityRecognizer": {
      "url": "https://api.wit.ai",
      "resource": "message",
      "serverAccessToken": "SERVER_ACCESS_TOKEN",
      "version": "20180811"
    }
  }
}

