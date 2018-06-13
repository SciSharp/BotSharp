using BotSharp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyProvider : INlpProvider
    {
        public async void LoadModel()
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("", new JsonContent(new { }));
            }
        }

        public object GetDoc()
        {
            throw new NotImplementedException();
        }
    }
}
