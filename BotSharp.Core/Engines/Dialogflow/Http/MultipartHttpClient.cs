using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace BotSharp.Core.Engines.Dialogflow.Http
{
    public class MultipartHttpClient
    {
        private const string delimiter = "--";
        private string boundary = "SwA" + DateTime.UtcNow.Ticks.ToString("x") + "SwA";
        private HttpWebRequest request;
        private BinaryWriter os;

        public MultipartHttpClient(HttpWebRequest request)
        {
            this.request = request;
        }

        public void connect()
        {
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.SendChunked = true;
            request.KeepAlive = true;

            os = new BinaryWriter(request.GetRequestStream(), Encoding.UTF8);
        }

        public void addStringPart(string paramName, string data)
        {
            WriteString(delimiter + boundary + "\r\n");
            WriteString("Content-Type: application/json\r\n");
            WriteString("Content-Disposition: form-data; name=\"" + paramName + "\"\r\n");
            WriteString("\r\n" + data + "\r\n");
        }

        public void addFilePart(string paramName, string fileName, Stream data)
        {
            WriteString(delimiter + boundary + "\r\n");
            WriteString("Content-Disposition: form-data; name=\"" + paramName + "\"; filename=\"" + fileName + "\"\r\n");
            WriteString("Content-Type: audio/wav\r\n");

            WriteString("\r\n");

            int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];

            int bytesActuallyRead;

            bytesActuallyRead = data.Read(buffer, 0, bufferSize);
            while (bytesActuallyRead > 0)
            {
                os.Write(buffer, 0, bytesActuallyRead);
                bytesActuallyRead = data.Read(buffer, 0, bufferSize);
            }

            WriteString("\r\n");
        }

        public void finish()
        {
            WriteString(delimiter + boundary + delimiter + "\r\n");
            os.Close();
        }

        private void WriteString(string str)
        {
            os.Write(Encoding.UTF8.GetBytes(str));
        }

        public string getResponse()
        {
            try
            {
                var httpResponse = request.GetResponse() as HttpWebResponse;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    return result;
                }
            }
            catch (WebException we)
            {
                using (var stream = we.Response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
