using System.Collections.Generic;
using System.Text;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using RedBlack.Library;
using RedBlack.Library.DataAccess;
using RedBlack.Library.DataContracts;
using Slight.Alexa.Framework.Models.Requests;
using Slight.Alexa.Framework.Models.Requests.RequestTypes;
using Slight.Alexa.Framework.Models.Responses;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace RedBlack.Alexa
{
    public class Function
    {
        private IDealer _dealer;

        public Function()
        {
            Init();
        }

        private void Init()
        {
            var services = new ServiceCollection();
            services.AddTransient<IGameRepository, DynamoDBGameRepository>();
            services.AddTransient<IDealer, Dealer>();

            var provider = services.BuildServiceProvider();

            _dealer = provider.GetService<IDealer>();
        }

        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            IOutputSpeech innerResponse = null;
            var log = context.Logger;
            var shouldEndSession = false;

            if (input.GetRequestType() == typeof(ILaunchRequest))
            {
                // default launch request, let's just let them know what you can do
                log.LogLine("Default LaunchRequest made");

                // grab the existing game if there is one
                var currentGame = _dealer.FindGame(input.Session.User.UserId);

                if (currentGame != null)
                {
                    innerResponse = new PlainTextOutputSpeech
                    {
                        Text = $"Welcome back to Red or Black. Your current game has {currentGame.cardsRemainingCount} cards remaining with a score of {currentGame.score}. You can either continue this game by making a guess, or ask me to start a new game."
                    };
                }
                else
                {
                    innerResponse = new PlainTextOutputSpeech
                    {
                        Text = "Welcome to Red or Black. Ask me to start a game with you."
                    };
                }
            }
            else if (input.GetRequestType() == typeof(IIntentRequest))
            {
                // intent request, process the intent
                log.LogLine($"Intent Requested {input.Request.Intent.Name}");
                
                switch (input.Request.Intent.Name)
                {
                    case "NewGameIntent":
                        innerResponse = StartNewGame(input.Session.User.UserId);
                        break;
                    case "GuessTheCardIntent":
                        innerResponse = CheckGuess(input.Session.User.UserId, input.Request.Intent.Slots);
                        break;
                    case "AMAZON.StopIntent":
                        innerResponse = new PlainTextOutputSpeech
                        {
                            Text = "Goodbye. Thanks for playing"
                        };
                        shouldEndSession = true;
                        break;
                    default:
                        innerResponse = new PlainTextOutputSpeech
                        {
                            Text = "Hmm, something went wrong. Try again"
                        };
                        break;
                }
            }

            var response = new Response
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = innerResponse
            };

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };

            return skillResponse;
        }

        private IOutputSpeech StartNewGame(string playerId)
        {
            _dealer.StartGame(playerId);

            return new PlainTextOutputSpeech
            {
                Text = "Alright, the deck's shuffled. Take a guess?"
            };
        }

        private IOutputSpeech CheckGuess(string playerId, Dictionary<string, Slot> intentSlots)
        {
            var colour = intentSlots["Colour"].Value;
            var number = intentSlots["Value"].Value;
            var suit = intentSlots["Suit"].Value;

            var assumption = new Assumption(colour, number, suit);

            if (assumption.IsValid)
            {
                var outcome = _dealer.TakeTurn(playerId, assumption);

                if (outcome is TurnErrorOutcome)
                {
                    var errorOutcome = outcome as TurnErrorOutcome;
                    return new PlainTextOutputSpeech
                    {
                        Text = errorOutcome.ErrorReason
                    };
                }
                else if (outcome is TurnSuccessOutcome)
                {
                    var successOutcome = outcome as TurnSuccessOutcome;
                    
                    var message = new StringBuilder();

                    message.Append(successOutcome.AssumptionResult.Success ? "Nice one. " : "Nope. ");

                    message.Append($"{successOutcome.DrawnCard.value} of {successOutcome.DrawnCard.suit}. ");

                    var game = successOutcome.AssumptionResult.GameState;
                    message.Append($"Your score is {game.score} with {successOutcome.AssumptionResult.GameState.cardsRemainingCount} cards remaining. ");

                    message.Append(successOutcome.AssumptionResult.GameState.cardsRemainingCount == 0
                        ? $"No more cards in the deck. Your final score was {game.score}. Ask me to start a new game? "
                        : "Take a guess? ");

                    return new PlainTextOutputSpeech
                    {
                        Text = message.ToString()
                    };
                }

                return new PlainTextOutputSpeech
                {
                    Text = "Hmm, something went wrong. Try again."
                };
            }

            return new PlainTextOutputSpeech
            {
                Text = "I don't know what you mean. I'm just here to deal the cards. Try again."
            };
        }
    }
}
