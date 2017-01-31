namespace RedBlack.DataContracts
{
    public class DrawCardResponse
    {
        public int remaining { get; set; }

        public bool success { get; set; }

        public string deck_id { get; set; }

        public Card[] cards{ get; set; }
    }
}