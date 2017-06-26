using System;
using System.Linq;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using RedBlack.Library.DataContracts;
using RedBlack.Library;
using Microsoft.Extensions.DependencyInjection;
using RedBlack.Library.DataAccess;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace RedBlack
{
    public class Function
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="inputObject"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string FunctionHandler(object inputObject, ILambdaContext context)
        {
            Init();

            //The incoming is of type object so I can log the exact object coming in. I then try to deserialize into the expected object
            var serialized = JsonConvert.SerializeObject(inputObject);
            LambdaLogger.Log(serialized);
            
            var input = JsonConvert.DeserializeObject<IncomingRequest>(serialized);

            foreach (var entry in input.bodyjson.entry)
            {
                foreach (var message in entry.messaging)
                {
                    var senderId = message.sender.id;
                    var messageText = message.message.text;

                    HandleIncomingMessage(senderId, messageText);
                }
            }

            return "";
        }

        private static void Init()
        {
            var services = new ServiceCollection();
            services.AddScoped<IGameRepository, DynamoDBGameRepository>();

            _serviceProvider = services.BuildServiceProvider();
        }

        private static void HandleIncomingMessage(string recipientId, string messageText)
        {
            var text = messageText.ToLower();

            var dealer = new Dealer(_serviceProvider.GetService<IGameRepository>());
            if (text.StartsWith("start game"))
            {
                dealer.StartGame(recipientId);

                var message = "Alright the deck's shuffled.";
                Messenger.SendMessage(recipientId, message);

                message = "Take a guess";
                Messenger.SendMessage(recipientId, message);
                return;
            }

            var assumption = new Assumption(text);
            if (assumption.IsValid)
            {
                var outcome = dealer.TakeTurn(recipientId, assumption);

                if (outcome is TurnErrorOutcome)
                {
                    var errorOutcome = outcome as TurnErrorOutcome;
                    Messenger.SendMessage(recipientId, errorOutcome.ErrorReason);
                }
                else if (outcome is TurnSuccessOutcome)
                {
                    var successOutcome = outcome as TurnSuccessOutcome;
                    SendCardToPlayer(recipientId, successOutcome.DrawnCard);

                    string message;

                    var game = successOutcome.AssumptionResult.GameState;
                    if (successOutcome.AssumptionResult.Success)
                    {
                        message = $"Nice one. \nYour score is {game.score} with {successOutcome.AssumptionResult.GameState.cardsRemainingCount} cards remaining.";
                    }
                    else
                    {
                        message = $"Nope. \nYour score is {game.score} with {successOutcome.AssumptionResult.GameState.cardsRemainingCount} cards remaining.";
                    }

                    Messenger.SendMessage(recipientId, message);

                    if (successOutcome.AssumptionResult.GameState.cardsRemainingCount == 0)
                    {
                        Messenger.SendMessage(recipientId, $"No more cards in the deck.\nFinal score: {game.score}");
                    }
                    else
                    {
                        Messenger.SendMessage(recipientId, "Take a guess");
                    }
                }
                else
                {
                    Messenger.SendMessage(recipientId, "Hmm something went wrong");
                }
            }
            else
            {
                Messenger.SendMessage(recipientId, "Huh? I'm just here to deal the cards");
            }
        }

        private static void SendCardToPlayer(string playerId, Card card)
        {
            Messenger.SendMessage(playerId, $"{card.value} of {card.suit.ToLower()}");
            //Messenger.SendImage(playerId, card.image).Wait();
        }
    }
}
