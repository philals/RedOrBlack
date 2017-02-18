namespace RedBlack.Library.DataContracts
{
    public class DrawCardResponse
    {
        public string error { get; set; }

        public int remaining { get; set; }

        public bool success { get; set; }

        public string deck_id { get; set; }

        public Card[] cards{ get; set; }
    }
}