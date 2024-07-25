using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace BotSharp.Abstraction.Utilities;

public static class JsonExtensions
{
    public static string FormatJson(this string? json, Formatting format = Formatting.Indented)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            var parsedJson = JObject.Parse(json);
            foreach (var item in parsedJson)
            {
                try
                {
                    var key = item.Key;
                    var value = parsedJson[key].ToString();
                    var parsedValue = JObject.Parse(value);
                    parsedJson[key] = parsedValue;
                }
                catch { continue; }
            }

            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = format
            };
            return JsonConvert.SerializeObject(parsedJson, jsonSettings);
        }
        catch
        {
            return json;
        }
    }
}
