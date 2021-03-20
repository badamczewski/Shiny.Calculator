using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiny.Calculator.Parsing
{
    [Serializable]
    public class ParsingException : Exception
    {
        public int Line { get; set; }
        public int Position { get; set; }

        public ParsingException(string message, int line, int position) : base(message)
        {
            Line = line;
            Position = position;
        }
    }
}
