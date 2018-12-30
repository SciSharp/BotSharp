using BotSharp.Platform.Abstraction;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.ContextStorage
{
    public class ContextStorageInFile<T> : IContextStorage<T>
    {
        private static string storageDir;

        public ContextStorageInFile()
        {
            IConfiguration config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var db = config.GetSection("Database:Default").Value;
            storageDir = config.GetSection($"Database:ConnectionStrings:{db}").Value;
            string contentDir = AppDomain.CurrentDomain.GetData("DataPath").ToString();
            storageDir = storageDir.Replace("|DataDirectory|", contentDir + Path.DirectorySeparatorChar + "SessionStorage" + Path.DirectorySeparatorChar);

            if (!Directory.Exists(storageDir))
            {
                Directory.CreateDirectory(storageDir);
            }
        }

        public async Task<T[]> Fetch(string sessionId)
        {
            string dataPath = Path.Combine(storageDir, sessionId + ".json");
            if (File.Exists(dataPath))
            {
                string json = File.ReadAllText(dataPath);
                return JsonConvert.DeserializeObject<T[]>(json);
            }
            else
            {
                return new T[0];
            }
        }

        public async Task<bool> Persist(string sessionId, T[] context)
        {
            var json = JsonConvert.SerializeObject(context, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                }
            });

            string dataPath = Path.Combine(storageDir, sessionId + ".json");

            File.WriteAllText(dataPath, json);

            return true;
        }
    }
}
