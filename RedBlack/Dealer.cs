using System;
using System.IO;
using System.Linq;
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
                var shuffledResponse = JsonConvert.DeserializeObject<ShuffledDeckResponse>(content);

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

            var message = "Alright the deck's shuffled.";
            Messenger.SendMessage(playerId, message).Wait();

            message = "Red or Black?";
            Messenger.SendMessage(playerId, message).Wait();
        }

        public async Task TakeTurn(string playerId, string assumption)
        {
            var currentGame = await RetrieveGame(playerId);
            var endpoint = $"{currentGame.deckId}/draw/?count=1";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{_baseUrl}/{endpoint}");
                var content = await response.Content.ReadAsStringAsync();
                var drawCardResponse = JsonConvert.DeserializeObject<DrawCardResponse>(content);

                var card = drawCardResponse.cards.First();
                var cardString = $"{card.value} of {card.suit.ToLower()}";

                Messenger.SendMessage(playerId, cardString).Wait();
                
                if (assumption.ToLower() == "red" &&
                    (card.suit.ToLower() == "diamonds" || card.suit.ToLower() == "hearts"))
                {
                    var message = $"Nice one. \nScore: {++currentGame.score}";
                    Messenger.SendMessage(playerId, message).Wait();
                }
                else if (assumption.ToLower() == "black" &&
                         (card.suit.ToLower() == "spades" || card.suit.ToLower() == "clubs"))
                {
                    var message = $"Nice one. \nScore: {++currentGame.score}";
                    Messenger.SendMessage(playerId, message).Wait();
                }
                else
                {
                    var message = $"Nope. \nScore: {currentGame.score}";
                    Messenger.SendMessage(playerId, message).Wait();
                }


                using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.APSoutheast2))
                {
                    var putObjectRequest = new PutObjectRequest
                    {
                        BucketName = Environment.GetEnvironmentVariable("playerS3BucketName"),
                        Key = $"{currentGame.playerId}.json",
                        ContentBody = JsonConvert.SerializeObject(currentGame)
                    };

                    await s3Client.PutObjectAsync(putObjectRequest);
                }
            }

            Messenger.SendMessage(playerId, "Red or Black?").Wait();
        }

        private async Task<Game> RetrieveGame(string playerId)
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("playerS3BucketName"),
                Key = $"{playerId}.json"
            };

            using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.APSoutheast2))
            {
                var response = await s3Client.GetObjectAsync(getObjectRequest);

                using (var streamReader = new StreamReader(response.ResponseStream))
                {
                    var content = streamReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<Game>(content);
                }
            }
        }
    }
}
