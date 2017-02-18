using System;
using System.Collections.Generic;
using System.Linq;

namespace RedBlack.Library.DataContracts
{
    public class Assumption
    {
        public Assumption(string text)
        {
            text = text.ToLower();

            BuildAssumption(text);
            if (!IsValid)
            {
                return;
            }

            ValidateAssumption();
            if (!IsValid)
            {
                return;
            }

            CalculateWorth();
        }

        public bool IsValid { get; set; } = true;

        public int Worth { get; set; }

        public string Colour { get; set; }
        public bool HasColour => !string.IsNullOrEmpty(Colour);

        public string Number { get; set; }
        public bool HasNumber => !string.IsNullOrEmpty(Number);

        public string Suit { get; set; }
        public bool HasSuit => !string.IsNullOrEmpty(Suit);

        protected List<string> Colours => new List<string> {"red", "black"};
        protected List<string> Suits => new List<string> {"hearts", "diamonds", "spades", "clubs"};
        protected List<string> Numbers => new List<string> {"ace", "2", "two", "3", "three", "4", "four", "5", "five", "6", "six", "7","seven", "8", "eight", "9", "nine", "10", "ten", "jack", "queen", "king" };

        private void BuildAssumption(string text)
        {
            Colour = FindValue(text, Colours);
            Number = FindValue(text, Numbers);
            Suit = FindValue(text, Suits);
        }

        public bool IsCorrect(Card card)
        {
            var number = card.value.ToLower();
            var suit = card.suit.ToLower();
            var colour = (suit == "hearts" || suit == "diamonds") ? "red" : "black";

            if (HasNumber && !string.Equals(Number, number, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if (HasColour && !string.Equals(Colour, colour, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if (HasSuit && !string.Equals(Suit, suit, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private string FindValue(string text, List<string> valueList)
        {
            var containedValues = valueList.Where(text.Contains).ToList();
            if (containedValues.Count > 1)
            {
                IsValid = false;
            }
            else if (containedValues.Count == 1)
            {
                return containedValues.First();
            }

            return null;
        }

        private void ValidateAssumption()
        {
            // Need at least one value populated
            if (!HasNumber && !HasColour && !HasSuit)
            {
                IsValid = false;
            }

            //but not both of suit and colour
            if (HasSuit && HasColour)
            {
                IsValid = false;
            }
        }

        private void CalculateWorth()
        {
            //Colour only
            if (HasColour && !HasNumber && !HasSuit)
            {
                Worth = 2;
            }

            //Suit only
            if (HasSuit && !HasNumber && !HasColour)
            {
                Worth = 4;
                return;
            }

            //Number only
            if (HasNumber && !HasSuit && !HasColour)
            {
                Worth = 13;
                return;
            }

            //Number and Colour
            if (HasNumber && HasColour && !HasSuit)
            {
                Worth = 26;
            }

            //Number and suit
            if (HasNumber && HasSuit && !HasColour)
            {
                Worth = 52;
            }
        }
    }
}