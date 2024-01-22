# LLM Provider

`BotSharp` can support multiple LLM providers through plug-ins, one `provider` could contain several `model` settings.
```json
[{
  "Provider": "azure-openai",
  "Models": [
    {
      "Id": "",
      "Name": "gpt-35-turbo",
      "Group": "",
      "ApiKey": "",
      "Endpoint": "https://gpt-35-turbo.openai.azure.com/",
      "Type": "chat",
      "PromptCost": 0.0015,
      "CompletionCost": 0.002
    },
    {
      "Name": "gpt-35-turbo-instruct",
      "ApiKey": "",
      "Endpoint": "https://gpt-35-turbo-instruct.openai.azure.com/",
      "Type": "text",
      "PromptCost": 0.0015,
      "CompletionCost": 0.002
    }
  ]
}]
```

You can set the names of `Provider` and `Model` in each round of dialogue to control the LLM that should be used in the current dialogue, or you can also specify the LLM used in subsequent dialogues once during dialogue initialization.

```json
{
  "text": "Good morning!",
  "provider": "google-ai",
  "model": "palm2"
}
```

## Load balancing

If you have deployed models with the same functions in multiple regions and want to establish a load balance among these regions to reduce the resource constraints of large model providers, you need to set a consistent Group value in the model configuration.