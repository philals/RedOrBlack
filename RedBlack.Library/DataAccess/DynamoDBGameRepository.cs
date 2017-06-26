using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using RedBlack.Library.DataContracts;

namespace RedBlack.Library.DataAccess
{
    public class DynamoDBGameRepository : IGameRepository
    {
        public bool SaveGame(Game gameData)
        {
            var putItemRequest = new PutItemRequest
            {
                TableName = "Game",
                Item = gameData.AsDictionary()
            };

            using (var dynamoClient = new AmazonDynamoDBClient(RegionEndpoint.APSoutheast2))
            {
                PutItemResponse response = null;

                Task.Run(async () =>
                {
                    response = await dynamoClient.PutItemAsync(putItemRequest);
                }).Wait();

                return (response?.HttpStatusCode ?? HttpStatusCode.InternalServerError) == HttpStatusCode.OK;
            }
        }

        public Game FindGame(string playerId)
        {
            var getItemRequest = new GetItemRequest
            {
                TableName = "Game",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PlayerId", new AttributeValue {S = playerId} }
                }
            };

            using (var dynamoClient = new AmazonDynamoDBClient(RegionEndpoint.APSoutheast2))
            {
                GetItemResponse response = null;

                Task.Run(async () =>
                {
                    response = await dynamoClient.GetItemAsync(getItemRequest);
                }).Wait();

                try
                {
                    var gameData = new Game
                    {
                        playerId = response.Item["PlayerId"].S,
                        deckId = response.Item["DeckId"].S,
                        score = int.Parse(response.Item["Score"].N),
                        cardsRemainingCount = int.Parse(response.Item["CardsRemainingCount"].N)
                    };

                    return gameData;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}