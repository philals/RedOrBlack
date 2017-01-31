namespace RedBlack.DataContracts
{
    public class Message
    {
        public string mid { get; set; }
        public string seq { get; set; }
        public string text { get; set; }
    }

    public class OutboundMessage
    {
        public string text { get; set; }
    }
}