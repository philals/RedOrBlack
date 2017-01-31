using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using RedBlack.DataContracts;

namespace RedBlack
{
    public class Dealer
    {
        private string _baseUrl = "https://deckofcardsapi.com/api/deck";

        public async Task StartGame(string playerId)
        {
            var endpoint = "new/shuffle/?deck_count=1";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{_baseUrl}/{endpoint}");
                var content = await response.Content.ReadAsStringAsync();
                var shuffledResponse = JsonConvert.DeserializeObject<ShuffledCardsResponse>(content);

                using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.APSoutheast2))
                {
                    var game = new Game {playerId = playerId, deckId = shuffledResponse.deck_id, score = 0};
                    var putObjectRequest = new PutObjectRequest
                    {
                        BucketName = Environment.GetEnvironmentVariable("playerS3BucketName"),
                        Key = $"{game.playerId}.json",
                        ContentBody = JsonConvert.SerializeObject(game)
                    };

                    await s3Client.PutObjectAsync(putObjectRequest);
                }
            }

            var outboundRequest = new OutboundRequest
            {
                recipient = new Recipient { id = playerId },
                message = new OutboundMessage { text = "Alright the deck's shuffled." }
            };

            Messenger.SendMessage(outboundRequest).Wait();

            outboundRequest.message.text = "Red or Black?";
            Messenger.SendMessage(outboundRequest).Wait();
        }
    }
}
