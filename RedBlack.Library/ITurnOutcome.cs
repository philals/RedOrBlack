using RedBlack.Library.DataContracts;

namespace RedBlack.Library
{
    public interface ITurnOutcome
    {
    }

    public class TurnErrorOutcome : ITurnOutcome
    {
        public string ErrorReason { get; set; }
    }

    public class TurnSuccessOutcome : ITurnOutcome
    {
        public Card DrawnCard { get; set; }

        public AssumptionResult AssumptionResult { get; set; }

        public int RemainingCardCount { get; set; }
    }
}
