using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace RedBlack.Library.DataContracts
{
    public class Game
    {
        public string playerId { get; set; }
        public string deckId { get; set; }
        public int score { get; set; }
        public int cardsRemainingCount { get; set; }
    }

    public static class GameExtensions
    {
        public static Dictionary<string, AttributeValue> AsDictionary(this Game game)
        {
            return new Dictionary<string, AttributeValue>
            {
                { "PlayerId", new AttributeValue {S = game.playerId} },
                { "DeckId", new AttributeValue {S = game.deckId} },
                { "Score", new AttributeValue {N = game.score.ToString()} },
                { "CardsRemainingCount", new AttributeValue {N = game.cardsRemainingCount.ToString()} }
            };
        }
    }
}