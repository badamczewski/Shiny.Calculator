using Shiny.Calculator.Parsing;
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

    public class BlockState : EvaluatorState
    {
        public BlockExpression Block;
        public Enviroment Enviroment;
    }

    public class ErrorState : EvaluatorState
    {
        
    }
}
