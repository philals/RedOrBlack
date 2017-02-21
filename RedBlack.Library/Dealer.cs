using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using RedBlack.Library.DataAccess;
using RedBlack.Library.DataContracts;

namespace RedBlack.Library
{
    public interface IDealer
    {
        Task StartGame(string playerId);
        Task TakeTurn(string playerId, Assumption assumption);
    }

    public class Dealer : IDealer
    {
        private readonly IGameRepository _gameRepo;

        public Dealer(IGameRepository gameRepo)
        {
            _gameRepo = gameRepo;
        }

        private const string BaseUrl = "https://deckofcardsapi.com/api/deck";

        public async Task StartGame(string playerId)
        {
            var shuffledResponse = await GetNewDeck();

            var gameData = new Game { playerId = playerId, deckId = shuffledResponse.deck_id, score = 0 };
            SaveGame(gameData);

            var message = "Alright the deck's shuffled.";
            Messenger.SendMessage(playerId, message).Wait();

            message = "Take a guess";
            Messenger.SendMessage(playerId, message).Wait();
        }

        public async Task TakeTurn(string playerId, Assumption assumption)
        {
            var currentGame = FindGame(playerId);

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

            SaveGame(currentGame);

            if (drawCardResponse.remaining == 0)
            {
                EndGame(currentGame);
            }
            else
            {
                Messenger.SendMessage(playerId, "Take a guess").Wait();
            }
        }

        private static async void EndGame(Game currentGame)
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
                message = $"Nice one. \nYour score is {currentGame.score} with {drawCardResponse.remaining} cards remaining.";
            }
            else
            {
                message = $"Nope. \nYour score is {currentGame.score} with {drawCardResponse.remaining} cards remaining.";
            }
            Messenger.SendMessage(currentGame.playerId, message).Wait();

            return currentGame;
        }

        private Game FindGame(string playerId)
        {
            return _gameRepo.FindGame(playerId);
        }

        private void SaveGame(Game gameData)
        {
            _gameRepo.SaveGame(gameData);
        }
    }
}
