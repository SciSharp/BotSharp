using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BotSharp.Abstraction.Utilities
{
    public static class ObjectExtensions
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
        private static JsonSerializerSettings DefaultSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static T DeepClone<T>(this T obj) where T : class
        {
            if (obj == null) return null;

            try
            {
                var json = JsonSerializer.Serialize(obj, DefaultOptions);
                return JsonSerializer.Deserialize<T>(json, DefaultOptions);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"DeepClone Error:{ex}");
                return DeepCloneWithNewtonsoft(obj);
            }            
        }

        private static T DeepCloneWithNewtonsoft<T>(this T obj) where T : class
        {
            if (obj == null) return null;

            try
            {
                var json = JsonConvert.SerializeObject(obj, DefaultSettings);
                return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"DeepClone Error:{ex}");
                return obj;
            }            
        }
    }
}
