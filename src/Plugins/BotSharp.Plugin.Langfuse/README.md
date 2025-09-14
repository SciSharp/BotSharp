# BotSharp.Plugin.Langfuse

This plugin integrates BotSharp with Langfuse, an open-source LLM observability platform.

## Features

- Tracks conversation flows and LLM interactions
- Monitors token usage for cost analysis
- Logs function executions
- Manages user sessions
- Captures comprehensive metadata

## Configuration

```json
{
  "Langfuse": {
    "Enabled": true,
    "PublicKey": "pk-lf-your-public-key",
    "SecretKey": "sk-lf-your-secret-key",
    "Host": "https://cloud.langfuse.com",
    "LogConversations": true,
    "LogFunctions": true,
    "LogTokenStats": true
  }
}
```

## Getting Started

1. Create a Langfuse account at [langfuse.com](https://langfuse.com/)
2. Get your project keys from the Langfuse dashboard
3. Add the configuration to your `appsettings.json`
4. Enable the plugin and start monitoring your conversations

For detailed setup instructions, see the [documentation](../../docs/integrations/langfuse.md).

## Dependencies

- `zborek.LangfuseDotnet` - .NET client for Langfuse API
- `BotSharp.Abstraction` - Core BotSharp abstractions