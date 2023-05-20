using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ChatGPT.Models;

public class ListResponse<T>
{
    public int Total { get; set; }
    public int Offset { get; set; }
    [JsonProperty("has_missing_conversations")]
    public bool HasMissingConversations { get; set; }
    public List<T> Items { get; set; } = new List<T>();
}
