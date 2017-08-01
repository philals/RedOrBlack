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
        void StartGame(string playerId);
        ITurnOutcome TakeTurn(string playerId, Assumption assumption);
        Game FindGame(string playerId);
    }

    public class Dealer : IDealer
    {
        private readonly IGameRepository _gameRepo;

        public Dealer(IGameRepository gameRepo)
        {
            _gameRepo = gameRepo;
        }

        private const string BaseUrl = "https://deckofcardsapi.com/api/deck";

        public void StartGame(string playerId)
        {
            var shuffledResponse = GetNewDeck().Result;

            var gameData = new Game { playerId = playerId, deckId = shuffledResponse.deck_id, score = 0, cardsRemainingCount = shuffledResponse.remaining};
            SaveGame(gameData);
        }

        public ITurnOutcome TakeTurn(string playerId, Assumption assumption)
        {
            var currentGame = FindGame(playerId);

            if (currentGame == null)
            {
                return new TurnErrorOutcome
                {
                    ErrorReason = "You don't currently have a game running. You need to start a new game"
                };
            }

            var drawCardResponse = DrawCard(currentGame).Result;

            if (!string.IsNullOrEmpty(drawCardResponse.error))
            {
                if (drawCardResponse.error.Contains("Not enough cards remaining to draw"))
                {
                    return new TurnErrorOutcome
                    {
                        ErrorReason = "There's no cards left. You need to start a new game."
                    };
                }
                else
                {
                    LambdaLogger.Log(drawCardResponse.error);
                    return new TurnErrorOutcome
                    {
                        ErrorReason = "Huh, something went wrong"
                    };
                }
            }

            currentGame.cardsRemainingCount = drawCardResponse.remaining;

            var assumptionResult = CheckAssumption(assumption, currentGame, drawCardResponse);

            SaveGame(assumptionResult.GameState);

            return new TurnSuccessOutcome
            {
                AssumptionResult = assumptionResult, 
                DrawnCard = drawCardResponse.cards.First()
            };
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

        

        private static AssumptionResult CheckAssumption(Assumption assumption, Game currentGame, DrawCardResponse drawCardResponse)
        {
            var card = drawCardResponse.cards.First();

            var result = new AssumptionResult
            {
                Success = assumption.IsCorrect(card),
                GameState = currentGame
            };

            if (result.Success)
            {
                result.GameState.score += assumption.Worth;
            }

            return result;
        }

        public Game FindGame(string playerId)
        {
            return _gameRepo.FindGame(playerId);
        }

        private void SaveGame(Game gameData)
        {
            _gameRepo.SaveGame(gameData);
        }
    }
}
