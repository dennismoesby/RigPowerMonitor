using RigPowerMonitor.Api.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RigPowerMonitor.Api.Handlers
{
    public class TextbeltHandler
    {
        public string ApiKey { get; set; }
        public string PhoneNumber { get; set; }

        public TextbeltHandler(string apiKey, string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException("apiKey", "Api key must not be NULL when instantiating Textbelthandler.");
                if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentNullException("phoneNumber", "Phone number must not be NULL when instantiating Textbelthandler.");

                ApiKey = apiKey;
                PhoneNumber = phoneNumber;
            }
            catch (RpmApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Failed to instantiate TextbeltHandler", ".ctor", RpmExceptionType.Exception, ex);
            }
        }


        public int SendText(string Message)
        {
            try
            {
                SendTextResult result = null;
                using (var client = new WebClient())
                {
                    byte[] response = client.UploadValues("http://textbelt.com/text", new NameValueCollection()
                    {
                        { "phone", PhoneNumber },
                        { "message", Message },
                        { "key", ApiKey }
                    });

                    var Jresult = System.Text.Encoding.UTF8.GetString(response);

                    result = JsonConvert.DeserializeObject<SendTextResult>(Jresult);

                    if (!result.success)
                        throw new RpmApiException($"Failed to send text message. Textbelt error: {result.error}.", "TextbeltHandler.SendText");
                }

                return result.quotaRemaining;
            }
            catch (RpmApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Failed to send text message", "Textbelthandler.SendText", RpmExceptionType.Exception, ex);
            }
        }

        private class SendTextResult
        {
            public bool success { get; set; }
            public int quotaRemaining { get; set; }
            public string textId { get; set; }
            public string error { get; set; }

        }
    }
}
