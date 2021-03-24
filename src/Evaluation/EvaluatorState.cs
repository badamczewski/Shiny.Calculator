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

    public class BlockContext
    {
        public Dictionary<string, int> Labels = new Dictionary<string, int>();
        public Dictionary<string, EvaluatorState> Variables = new Dictionary<string, EvaluatorState>()
        {
            { "res", new EvaluatorState() { Value = null, Type = LiteralType.Any } }
        };
        public int PC;
    }

}
