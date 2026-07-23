using OpenAI.Chat;

namespace BotSharp.Plugin.MiniMaxAI.Providers.Chat;

#pragma warning disable SCME0001

public class ChatCompletionProvider : global::BotSharp.Plugin.OpenAI.Providers.Chat.ChatCompletionProvider
{
    public override string Provider => "minimax";
    protected override bool UseResponseApi => false;

    public ChatCompletionProvider(
        ILogger<global::BotSharp.Plugin.OpenAI.Providers.Chat.ChatCompletionProvider> logger,
        IServiceProvider services,
        IConversationStateService state,
        IFileStorageService fileStorage) : base(new(), logger, services, state, fileStorage)
    {
    }

    protected override void ConfigureChatCompletionOptions(
        ChatCompletionOptions options,
        LlmModelSetting? settings)
    {
        var parameters = settings?.Reasoning?.Parameters;
        var thinkingType = LlmUtility.GetModelParameter(
            parameters,
            "ThinkingType",
            _state.GetState("thinking_type"));
        if (thinkingType is "adaptive" or "disabled")
        {
            var thinking = JsonSerializer.SerializeToUtf8Bytes(new { type = thinkingType });
            options.Patch.Set("$.thinking"u8, BinaryData.FromBytes(thinking));
        }

        var serviceTiers = settings?.Cost?.TextTokenCostTiers?
            .Select(x => x.ServiceTier)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var serviceTier = LlmUtility.VerifyModelParameter(
            _state.GetState("service_tier"),
            settings?.Cost?.DefaultServiceTier,
            serviceTiers);
        if (!string.IsNullOrWhiteSpace(serviceTier))
        {
            options.Patch.Set("$.service_tier"u8, serviceTier);
        }
    }
}
