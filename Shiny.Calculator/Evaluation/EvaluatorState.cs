using Shiny.Repl.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiny.Calculator.Evaluation
{
    public class EvaluatorState
    {
        public LiteralType Type;
        public string Value;
        public bool IsSigned;

        public static EvaluatorState Empty() { return new EvaluatorState(); }
    }

}
