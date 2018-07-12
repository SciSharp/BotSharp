using BotSharp.Core.Engines.Dialogflow.Http;
using BotSharp.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace BotSharp.Core.Engines.Dialogflow
{
    public class AIDataService
    {
        private readonly AIConfiguration config;

        public string SessionId { get; }

        public AIDataService(AIConfiguration config)
        {
            this.config = config;

            if (string.IsNullOrEmpty(config.SessionId))
            {
                SessionId = Guid.NewGuid().ToString();
            }
            else
            {
                SessionId = config.SessionId;
            }
        }

        public AIResponse Request(AIRequest request)
        {
            request.Language = config.Language.code;
            request.Timezone = TimeZone.CurrentTimeZone.StandardName;
            request.SessionId = SessionId;

            try
            {
                var httpRequest = (HttpWebRequest)WebRequest.Create(config.RequestUrl);
                httpRequest.Method = "POST";
                httpRequest.ContentType = "application/json; charset=utf-8";
                httpRequest.Accept = "application/json";

                httpRequest.Headers.Add("Authorization", "Bearer " + config.ClientAccessToken);

                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var jsonRequest = JsonConvert.SerializeObject(request, Formatting.None, jsonSettings);

                if (config.DebugLog)
                {
                    Debug.WriteLine("Request: " + jsonRequest);
                }

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonRequest);
                    streamWriter.Close();
                }

                var httpResponse = httpRequest.GetResponse() as HttpWebResponse;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();

                    if (config.DebugLog)
                    {
                        Debug.WriteLine("Response: " + result);
                    }

                    var aiResponse = JsonConvert.DeserializeObject<AIResponse>(result);

                    CheckForErrors(aiResponse);

                    return aiResponse;
                }

            }
            catch (Exception e)
            {
                throw new AIServiceException(e);
            }
        }

        public AIResponse VoiceRequest(Stream voiceStream, RequestExtras requestExtras = null)
        {
            var request = new AIRequest();
            request.Language = config.Language.code;
            request.Timezone = TimeZone.CurrentTimeZone.StandardName;
            request.SessionId = SessionId;

            if (requestExtras != null)
            {
                requestExtras.CopyTo(request);
            }

            try
            {
                var httpRequest = (HttpWebRequest)WebRequest.Create(config.RequestUrl);
                httpRequest.Method = "POST";
                httpRequest.Accept = "application/json";

                httpRequest.Headers.Add("Authorization", "Bearer " + config.ClientAccessToken);

                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var jsonRequest = JsonConvert.SerializeObject(request, Formatting.None, jsonSettings);

                if (config.DebugLog)
                {
                    Debug.WriteLine("Request: " + jsonRequest);
                }

                var multipartClient = new MultipartHttpClient(httpRequest);
                multipartClient.connect();

                multipartClient.addStringPart("request", jsonRequest);
                multipartClient.addFilePart("voiceData", "voice.wav", voiceStream);

                multipartClient.finish();

                var responseJsonString = multipartClient.getResponse();

                if (config.DebugLog)
                {
                    Debug.WriteLine("Response: " + responseJsonString);
                }

                var aiResponse = JsonConvert.DeserializeObject<AIResponse>(responseJsonString);

                CheckForErrors(aiResponse);

                return aiResponse;

            }
            catch (Exception e)
            {
                throw new AIServiceException(e);
            }
        }

        public bool ResetContexts()
        {
            var cleanRequest = new AIRequest("empty_query_for_resetting_contexts");
            cleanRequest.ResetContexts = true;
            try
            {
                var response = Request(cleanRequest);
                return !response.IsError;
            }
            catch (AIServiceException e)
            {
                Debug.WriteLine("Exception while contexts clean." + e);
                return false;
            }
        }

        static void CheckForErrors(AIResponse aiResponse)
        {
            if (aiResponse == null)
            {
                throw new AIServiceException("API.AI response parsed as null. Check debug log for details.");
            }

            if (aiResponse.IsError)
            {
                throw new AIServiceException(aiResponse);
            }
        }
    }
}
