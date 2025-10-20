using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Utilities
{
    public static class ObjectExtensions
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public static T DeepClone<T>(this T obj) where T : class
        {
            if (obj == null) return null;

            var json = JsonSerializer.Serialize(obj, DefaultOptions);
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
    }
}
