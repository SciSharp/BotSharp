# Langfuse Integration for BotSharp

This plugin integrates BotSharp with [Langfuse](https://langfuse.com/), an open-source LLM observability platform, to provide comprehensive monitoring and analytics for your AI conversations.

## Features

- **Conversation Tracking**: Automatically tracks all conversation flows and LLM interactions
- **Token Usage Monitoring**: Records prompt and completion token counts for cost analysis
- **Function Call Logging**: Monitors function executions within conversations
- **Session Management**: Groups related conversations by user sessions
- **Metadata Collection**: Captures agent information, model details, and custom metadata

## Installation

The Langfuse plugin is included in BotSharp and uses the `zborek.LangfuseDotnet` NuGet package for Langfuse integration.

## Configuration

Add the following configuration section to your `appsettings.json`:

```json
{
  "Langfuse": {
    "Enabled": true,
    "PublicKey": "pk-lf-your-public-key",
    "SecretKey": "sk-lf-your-secret-key", 
    "Host": "https://cloud.langfuse.com",
    "LogConversations": true,
    "LogFunctions": true,
    "LogTokenStats": true,
    "SessionTimeoutSeconds": 3600
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | boolean | `false` | Whether Langfuse observability is enabled |
| `PublicKey` | string | `""` | Your Langfuse public key |
| `SecretKey` | string | `""` | Your Langfuse secret key |
| `Host` | string | `"https://cloud.langfuse.com"` | Langfuse server URL |
| `LogConversations` | boolean | `true` | Whether to log conversation details |
| `LogFunctions` | boolean | `true` | Whether to log function executions |
| `LogTokenStats` | boolean | `true` | Whether to log token usage statistics |
| `SessionTimeoutSeconds` | integer | `3600` | Session timeout in seconds |

## Getting Started

### 1. Create a Langfuse Account

1. Visit [Langfuse](https://langfuse.com/) and create an account
2. Create a new project in your Langfuse dashboard
3. Copy your project's public key and secret key

### 2. Configure BotSharp

Add your Langfuse credentials to your BotSharp configuration:

```json
{
  "Langfuse": {
    "Enabled": true,
    "PublicKey": "pk-lf-...",
    "SecretKey": "sk-lf-...",
    "Host": "https://cloud.langfuse.com"
  }
}
```

### 3. Enable the Plugin

The Langfuse plugin is automatically registered when enabled in configuration. No additional setup is required.

## How It Works

The Langfuse plugin implements the `IContentGeneratingHook` interface to intercept and log:

- **BeforeGenerating**: Creates traces for new conversations and logs context
- **AfterGenerated**: Records LLM responses and token usage
- **BeforeFunctionInvoked**: Logs function call starts
- **AfterFunctionInvoked**: Records function execution results

### Data Captured

For each conversation, Langfuse captures:

- **Agent Information**: Agent ID, name, and model configuration
- **User Context**: User ID and session information  
- **Message Flow**: Complete conversation history
- **Token Metrics**: Input/output token counts for cost tracking
- **Function Calls**: All function invocations and results
- **Metadata**: Custom tags and debugging information

## Security Considerations

- **Secret Key Protection**: Store your Langfuse secret key securely using environment variables or secure configuration
- **Data Privacy**: Review Langfuse's data handling policies for compliance with your privacy requirements
- **Self-Hosted Option**: Consider using a self-hosted Langfuse instance for sensitive data

## Environment Variables

You can also configure Langfuse using environment variables:

```bash
LANGFUSE_ENABLED=true
LANGFUSE_PUBLIC_KEY=pk-lf-your-public-key
LANGFUSE_SECRET_KEY=sk-lf-your-secret-key
LANGFUSE_HOST=https://cloud.langfuse.com
```

## Troubleshooting

### Plugin Not Logging

1. Verify `Enabled` is set to `true` in configuration
2. Check that public and secret keys are correctly configured
3. Ensure network connectivity to Langfuse host
4. Review BotSharp logs for Langfuse-related errors

### Missing Data

1. Confirm `LogConversations` and other logging flags are enabled
2. Verify the agent is properly configured with LLM settings
3. Check that conversations are flowing through the content generating hooks

### Authentication Errors

1. Verify your Langfuse public and secret keys are correct
2. Ensure your Langfuse project is active
3. Check for any IP restrictions in your Langfuse project settings

## Development

To extend the Langfuse integration:

1. Modify `LangfuseContentGeneratingHook.cs` to add custom logging logic
2. Update `LangfuseSettings.cs` to add new configuration options
3. Implement additional hooks if needed for other BotSharp events

## Resources

- [Langfuse Documentation](https://langfuse.com/docs)
- [BotSharp Hooks Documentation](../architecture/hooks.md)
- [zborek.LangfuseDotnet Package](https://www.nuget.org/packages/zborek.LangfuseDotnet/)