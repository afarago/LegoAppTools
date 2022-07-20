using System;
using System.Collections.Generic;
using System.Text;

namespace LegoAppToolsLib
{
    public class LegoAppToolException : Exception
    {
        public LegoAppToolException(string message)
            : base(message)
        {
        }

        public LegoAppToolException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
