namespace RedBlack.DataContracts
{
    public class OutboundRequest
    {
        public Recipient recipient { get; set; }

        public OutboundMessage message { get; set; }
    }
}