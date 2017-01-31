namespace RedBlack.DataContracts
{
    public class MessageContainer
    {
        public Sender sender { get; set; }
        public Recipient recipient { get; set; }
        public string timestamp { get; set; }
        public Message message { get; set; }
    }
}