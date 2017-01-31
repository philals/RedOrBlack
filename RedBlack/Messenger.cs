using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RedBlack.DataContracts;

namespace RedBlack
{
    public class Messenger
    {
        public static async Task SendImage(string recipientId, string imageUrl)
        {
            var outboundRequest = new OutboundRequest
            {
                recipient = new Recipient { id = recipientId },
                message = new OutboundMessage { attachment = new Attachment
                {
                    payload = new Payload {url = imageUrl}, type = "image"}
                }
            };

            await Send(outboundRequest);
        }

        public static async Task SendMessage(string recipientId, string messageText)
        {
            var outboundRequest = new OutboundRequest
            {
                recipient = new Recipient {id = recipientId},
                message = new OutboundMessage {text = messageText}
            };

            await Send(outboundRequest);
        }

        private static async Task Send(OutboundRequest outboundRequest)
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
                    var response = await client.PostAsync(url, content);

                    Console.WriteLine("Status: " + response.StatusCode);

                    if (response.Content != null)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
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
