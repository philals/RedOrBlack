using RedBlack.Library.DataContracts;
using Xunit;

namespace RedBlack.UnitTests
{
    public class AssumptionTests
    {
        [Theory]
        [InlineData("black", "black", null, null)]
        [InlineData("king", null, "king", null)]
        [InlineData("Hearts", null, null, "hearts")]
        [InlineData("Red 2", "red", "2", null)]
        [InlineData("2 of spades", null, "2", "spades")]
        public void assumptions_can_be_created_from_text(string text, string expectedColour, string expectedNumber, string expectedSuit)
        {
            var assumption = new Assumption(text);

            Assert.True(assumption.IsValid, "Expected the assumption to be valid by it was not");
            Assert.Equal(expectedColour, assumption.Colour);
            Assert.Equal(expectedNumber, assumption.Number);
            Assert.Equal(expectedSuit, assumption.Suit);
        }
    }
}
