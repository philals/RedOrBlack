using Newtonsoft.Json;

namespace RedBlack.Library.DataContracts
{
    public class IncomingRequest
    {
        [JsonProperty(PropertyName = "body-json")]
        public BodyJson bodyjson { get; set; }
    }
}