using System;
using Amazon.Lambda.Core;
using RedBlack.Library.DataContracts;
using RedBlack.Library;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace RedBlack
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string FunctionHandler(IncomingRequest input, ILambdaContext context)
        {
            Console.WriteLine(input);

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

        private static void HandleIncomingMessage(string recipientId, string messageText)
        {
            var text = messageText.ToLower();

            var dealer = new Dealer();
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
