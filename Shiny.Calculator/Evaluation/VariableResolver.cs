using Shiny.Repl.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using Expression = Shiny.Repl.Parsing.Expression;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public class VariableResolver
    {
        private Dictionary<string, EvaluatorState> variables = new Dictionary<string, EvaluatorState>();
        public Dictionary<string, EvaluatorState> Resolve(Expression expression)
        {
            variables.Clear();
            Visit(expression);
            return variables;
        }
        private void Visit(Expression expression)
        {
            if (expression is BinaryExpression operatorExpression)
            {
                EvaluateBinaryExpression(operatorExpression);
                return;
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                EvaluateUnaryExpression(unaryExpression);
                return;
            }
            else if (expression is LiteralExpression literalExpression)
            {
                EvaluateLiteralExpression(literalExpression);
                return;
            }
            else if (expression is IdentifierExpression identifierExpression)
            {
                variables.TryAdd(identifierExpression.Identifier, new EvaluatorState());
                return;
            }

            throw new ArgumentException($"Invalid Expression: '{expression.ToString()}'");
        }

        private EvaluatorState EvaluateLiteralExpression(LiteralExpression literalExpression)
        {
            return null;
        }

        private void EvaluateUnaryExpression(UnaryExpression unaryExpression)
        {
            Visit(unaryExpression.Left);
        }

        private void EvaluateBinaryExpression(BinaryExpression operatorExpression)
        {
            Visit(operatorExpression.Left);
            Visit(operatorExpression.Right);
        }
    }
}
