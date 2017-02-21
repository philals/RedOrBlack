using System;
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
                dealer.StartGame(recipientId).Wait();
                return;
            }

            var assumption = new Assumption(text);
            if (assumption.IsValid)
            {
                dealer.TakeTurn(recipientId, assumption).Wait();
            }
            else
            {
                Messenger.SendMessage(recipientId, "Huh? I'm just here to deal the cards").Wait();
            }
        }
    }
}
