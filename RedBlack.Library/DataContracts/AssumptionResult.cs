using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedBlack.Library.DataContracts
{
    public class AssumptionResult
    {
        public bool Success { get; set; }

        public Game GameState { get; set; }
    }
}
