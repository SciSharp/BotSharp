# LLM Provider

`BotSharp` can support multiple LLM providers through plug-ins.

You can set the names of `Provider` and `Model` in each round of dialogue to control the LLM that should be used in the current dialogue, or you can also specify the LLM used in subsequent dialogues once during dialogue initialization.

```json
{
  "text": "Good morning!",
  "provider": "google-ai",
  "model": "palm2"
}
```