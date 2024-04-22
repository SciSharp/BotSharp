using BotSharp.Abstraction.Messaging.Enums;
using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

/// <summary>
/// Coupon Template
/// https://developers.facebook.com/docs/messenger-platform/send-messages/template/coupon
/// </summary>
public class CouponTemplateMessage : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    [JsonProperty("rich_type")]
    public string RichType => RichTypeEnum.CouponTemplate;

    [JsonPropertyName("text")]
    [JsonProperty("text")]
    public string Text { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }

    [JsonPropertyName("template_type")]
    [JsonProperty("template_type")]
    public string TemplateType => TemplateTypeEnum.Coupon;

    [JsonPropertyName("coupon_code")]
    [JsonProperty("coupon_code")]
    public string CouponCode { get; set; }

    [JsonPropertyName("coupon_url")]
    [JsonProperty("coupon_url")]
    public string CouponUrl { get; set; }

    [JsonPropertyName("coupon_url_button_title")]
    [JsonProperty("coupon_url_button_title")]
    public string CouponUrlButtonTitle { get; set; } = "Shop now";

    [JsonPropertyName("coupon_pre_message")]
    [JsonProperty("coupon_pre_message")]
    public string CouponPreMessage { get; set; } = "Here's a deal just for you!";

    [JsonPropertyName("image_url")]
    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }

    [JsonPropertyName("payload")]
    [JsonProperty("payload")]
    public string Payload { get; set; }
}