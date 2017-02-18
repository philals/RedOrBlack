using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using RedBlack.Library.DataContracts;

namespace RedBlack.Library
{
    public class Dealer
    {
        private const string BaseUrl = "https://deckofcardsapi.com/api/deck";

        public async Task StartGame(string playerId)
        {
            var shuffledResponse = await GetNewDeck();

            var gameData = new Game { playerId = playerId, deckId = shuffledResponse.deck_id, score = 0 };
            SendGameDataToS3(gameData).Wait();

            var message = "Alright the deck's shuffled.";
            Messenger.SendMessage(playerId, message).Wait();

            message = "Take a guess";
            Messenger.SendMessage(playerId, message).Wait();
        }

        public async Task TakeTurn(string playerId, Assumption assumption)
        {
            var currentGame = await RetrieveGameFromS3(playerId);

            var drawCardResponse = await DrawCard(currentGame);

            if (!string.IsNullOrEmpty(drawCardResponse.error))
            {
                if (drawCardResponse.error.Contains("Not enough cards remaining to draw"))
                {
                    Messenger.SendMessage(playerId, "There's no cards left. Say 'start game' to start a new game.").Wait();
                    return;
                }
                else
                {
                    LambdaLogger.Log(drawCardResponse.error);
                    Messenger.SendMessage(playerId, "Huh, something went wrong").Wait();
                    return;
                }
            }

            SendCardToPlayer(playerId, drawCardResponse);

            currentGame = CheckAssumption(assumption, currentGame, drawCardResponse);

            await SendGameDataToS3(currentGame);

            if (drawCardResponse.remaining == 0)
            {
                EndGame(currentGame);
            }
            else
            {
                Messenger.SendMessage(playerId, "Take a guess").Wait();
            }
        }

        private async void EndGame(Game currentGame)
        {
            await Messenger.SendMessage(currentGame.playerId, $"Final score: {currentGame.score}");
        }

        private async Task<ShuffledDeckResponse> GetNewDeck()
        {
            var endpoint = "new/shuffle/?deck_count=1";

            HttpResponseMessage response;
            using (var client = new HttpClient())
            {
                response = await client.GetAsync($"{BaseUrl}/{endpoint}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var shuffledResponse = JsonConvert.DeserializeObject<ShuffledDeckResponse>(content);
            return shuffledResponse;
        }

        private async Task<DrawCardResponse> DrawCard(Game currentGame)
        {
            var endpoint = $"{currentGame.deckId}/draw/?count=1";

            HttpResponseMessage response;
            using (var client = new HttpClient())
            {
                response = await client.GetAsync($"{BaseUrl}/{endpoint}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var drawCardResponse = JsonConvert.DeserializeObject<DrawCardResponse>(content);
            return drawCardResponse;
        }

        private static void SendCardToPlayer(string playerId, DrawCardResponse drawCardResponse)
        {
            var card = drawCardResponse.cards.First();
            Messenger.SendMessage(playerId, $"{card.value} of {card.suit.ToLower()}").Wait();
            //Messenger.SendImage(playerId, card.image).Wait();
        }

        private static Game CheckAssumption(Assumption assumption, Game currentGame, DrawCardResponse drawCardResponse)
        {
            var card = drawCardResponse.cards.First();

            string message;
            if (assumption.IsCorrect(card))
            {
                currentGame.score += assumption.Worth;
                message = $"Nice one. \nYour score is {currentGame.score} with {drawCardResponse.remaining} cards left.";
            }
            else
            {
                message = $"Nope. \nYour score is {currentGame.score} with {drawCardResponse.remaining} cards left.";
            }
            Messenger.SendMessage(currentGame.playerId, message).Wait();

            return currentGame;
        }

        private async Task<Game> RetrieveGameFromS3(string playerId)
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

        private static async Task SendGameDataToS3(Game gameData)
        {
            using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.APSoutheast2))
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("playerS3BucketName"),
                    Key = $"{gameData.playerId}.json",
                    ContentBody = JsonConvert.SerializeObject(gameData)
                };

                await s3Client.PutObjectAsync(putObjectRequest);
            }
        }
    }
}
