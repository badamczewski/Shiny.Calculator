using Shiny.Repl.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using Expression = Shiny.Repl.Parsing.Expression;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public class Evaluator
    {
        private Dictionary<string, EvaluatorState> variables = null;
        private IPrinter printer;
        public EvaluatorState Evaluate(
            Expression expression,
            Dictionary<string, EvaluatorState> resolvedVariables,
            IPrinter printer)
        {
            this.printer = printer;
            variables = resolvedVariables;
            return Visit(expression);
        }

        private EvaluatorState Visit(Expression expression)
        {
            if (expression is BinaryExpression operatorExpression)
            {
                return EvaluateBinaryExpression(operatorExpression);
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                return EvaluateUnaryExpression(unaryExpression);
            }
            else if (expression is LiteralExpression literalExpression)
            {
                return EvaluateLiteralExpression(literalExpression);
            }
            else if (expression is IdentifierExpression identifierExpression)
            {
                return variables[identifierExpression.Identifier];
            }

            throw new ArgumentException($"Invalid Expression: '{expression.ToString()}'");
        }

        private EvaluatorState EvaluateLiteralExpression(LiteralExpression literalExpression)
        {
            return new EvaluatorState() 
            { 
                Type = literalExpression.Type, 
                IsSigned = literalExpression.IsSigned, 
                Value = literalExpression.Value 
            };
        }

        private EvaluatorState EvaluateUnaryExpression(UnaryExpression unaryExpression)
        {
            var @operator = unaryExpression.Operator;
            var lhs = Visit(unaryExpression.Left);

            var result = EvalUnaryAsNumber(@operator, lhs);

            printer.PrintUnary(unaryExpression, lhs);

            return new EvaluatorState() { IsSigned = true, Type = lhs.Type, Value = result.ToString() };
        }

        private EvaluatorState EvaluateBinaryExpression(BinaryExpression operatorExpression)
        {
            var @operator = operatorExpression.Operator;
            var lhs = Visit(operatorExpression.Left);
            var rhs = Visit(operatorExpression.Right);

            printer.PrintBinary(operatorExpression, lhs, rhs);

            var result = EvalBinaryAsNumber(@operator, lhs, rhs);

            return new EvaluatorState() { IsSigned = lhs.IsSigned, Type = lhs.Type, Value = result.ToString() };
        }


        private long EvalBinaryAsNumber(string op, EvaluatorState lhs, EvaluatorState rhs)
        {
            switch (op)
            {
                case "+": return (long.Parse(lhs.Value) + long.Parse(rhs.Value));
                case "-": return (long.Parse(lhs.Value) - long.Parse(rhs.Value));
                case "*": return (long.Parse(lhs.Value) * long.Parse(rhs.Value));
                case "/": return (long.Parse(lhs.Value) / long.Parse(rhs.Value));

                case "&": return (long.Parse(lhs.Value) & long.Parse(rhs.Value));
                case "|": return (long.Parse(lhs.Value) | long.Parse(rhs.Value));
                case "^": return (long.Parse(lhs.Value) ^ long.Parse(rhs.Value));
                case "%": return (long.Parse(lhs.Value) % long.Parse(rhs.Value));
            }

            throw new ArgumentException("Invalid Operator");
        }
        private long EvalUnaryAsNumber(string op, EvaluatorState lhs)
        {
            switch (op)
            {
                case "-": return -(long.Parse(lhs.Value));
                case "~": return ~(long.Parse(lhs.Value));
            }

            throw new ArgumentException("Invalid Operator");
        }
    }
}
