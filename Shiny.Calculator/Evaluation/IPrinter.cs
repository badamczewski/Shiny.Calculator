using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using Expression = Shiny.Repl.Parsing.Expression;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public interface IPrinter
    {
        void Print(Expression expression, EvaluatorState state);
        void PrintBinary(BinaryExpression binaryExpression, EvaluatorState left, EvaluatorState right);
        void PrintUnary(UnaryExpression unaryExpression, EvaluatorState left);
    }

}
