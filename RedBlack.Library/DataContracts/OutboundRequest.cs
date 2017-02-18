namespace RedBlack.Library.DataContracts
{
    public class OutboundRequest
    {
        public Recipient recipient { get; set; }

        public OutboundMessage message { get; set; }
    }
}