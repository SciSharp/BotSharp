using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatGPT.Models;

public class ConversationPost
{
    [JsonProperty("action")]
    public string Action { get; set; } = "action";

    [JsonProperty("messages")]
    public List<ConversationMessageBody> Messages { get; set; }

    [JsonProperty("parent_message_id")]
    public string ParentMessageId { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("timezone_offset_min")]
    public int TimezoneOffsetMin { get; set; }

    [JsonProperty("variant_purpose")]
    public string VariantPurpose { get; set; } = "none";
}

public class ConversationMessageBody
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("author")]
    public ConversationAuthor Author { get; set; }

    [JsonProperty("content")]
    public ConversationContent Content { get; set; }
}

public class ConversationAuthor
{
    [JsonProperty("role")]
    public string Role { get; set; }
}

public class ConversationContent
{
    [JsonProperty("content_type")]
    public string ContentType { get; set; }

    [JsonProperty("parts")]
    public List<string> Parts { get; set; }
}
