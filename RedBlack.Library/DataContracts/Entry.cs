namespace RedBlack.Library.DataContracts
{
    public class Entry
    {
        public string id { get; set; }
        public string time { get; set; }
        public MessageContainer[] messaging { get; set; }
    }
}