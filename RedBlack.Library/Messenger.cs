using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RedBlack.Library.DataContracts;

namespace RedBlack.Library
{
    public class Messenger
    {
        public static void SendImage(string recipientId, string imageUrl)
        {
            var outboundRequest = new OutboundRequest
            {
                recipient = new Recipient { id = recipientId },
                message = new OutboundMessage { attachment = new Attachment
                {
                    payload = new Payload {url = imageUrl}, type = "image"}
                }
            };

            Send(outboundRequest);
        }

        public static void SendMessage(string recipientId, string messageText)
        {
            var outboundRequest = new OutboundRequest
            {
                recipient = new Recipient {id = recipientId},
                message = new OutboundMessage {text = messageText}
            };

            Send(outboundRequest);
        }

        private static void Send(OutboundRequest outboundRequest)
        {
            using (var client = new HttpClient())
            {
                var str = JsonConvert.SerializeObject(outboundRequest);
                Console.WriteLine("content: " + str);
                var content = new StringContent(str, Encoding.UTF8, "application/json");

                var pageToken = Environment.GetEnvironmentVariable("pageToken");
                Console.WriteLine("pageToken: " + pageToken);

                var url = "https://graph.facebook.com/v2.6/me/messages?access_token=" + pageToken;

                try
                {
                    var response = client.PostAsync(url, content).Result;

                    Console.WriteLine("Status: " + response.StatusCode);

                    if (response.Content != null)
                    {
                        var responseString = response.Content.ReadAsStringAsync().Result;
                        Console.WriteLine(responseString);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }
    }
}
