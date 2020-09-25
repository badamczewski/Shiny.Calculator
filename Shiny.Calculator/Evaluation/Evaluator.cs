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
    public class EvaluatorContext
    {
        public bool IsExplainAll;
        public bool IsExplain;
    }

    public class Evaluator
    {
        private Dictionary<string, EvaluatorState> variables = null;
        public EvaluatorContext context;

        private IPrinter printer;
        public EvaluatorState Evaluate(
            Expression expression,
            Dictionary<string, EvaluatorState> resolvedVariables,
            IPrinter printer, EvaluatorContext context)
        {
            this.context = context;
            this.printer = printer;
            variables = resolvedVariables;
            var result = Visit(expression);

            if (context.IsExplainAll) context.IsExplain = true;

            if (result.Value != null)
            {
                printer.Print(new Run() { Text = "  =", Color = RunColor.Red });
                PrintAsBitSet((int)long.Parse(result.Value));
            }

            return result;
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
            else if(expression is CommandExpression commandExpression)
            {
                if (commandExpression.CommandName == "explain")
                {
                    context.IsExplain = true;
                }
                else if (commandExpression.CommandName == "explain_on")
                {
                    context.IsExplainAll = true;
                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "explain_off")
                {
                    context.IsExplainAll = false;
                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "cls")
                {
                    printer.Clear();
                    return new EvaluatorState();
                }

                return Visit(commandExpression.RightHandSide);
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

            if (context.IsExplain)
            {
                PrintAsBitSet((int)long.Parse(lhs.Value));
            }

            return new EvaluatorState() { IsSigned = true, Type = lhs.Type, Value = result.ToString() };
        }

        private EvaluatorState EvaluateBinaryExpression(BinaryExpression operatorExpression)
        {
            var @operator = operatorExpression.Operator;
            var lhs = Visit(operatorExpression.Left);
            var rhs = Visit(operatorExpression.Right);

            if (context.IsExplain)
            {
                PrintAsBitSet((int)long.Parse(lhs.Value));
                printer.Print(new Run() { Text = "  " + operatorExpression.Operator, Color = RunColor.Red });
                PrintAsBitSet((int)long.Parse(rhs.Value));
            }

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
                case ">>": return (long.Parse(lhs.Value) >> int.Parse(rhs.Value));
                case "<<": return (long.Parse(lhs.Value) << int.Parse(rhs.Value));

                //Logical shift pad with zeros
                case ">>>": return LogicalShift(long.Parse(lhs.Value), int.Parse(rhs.Value));
            }

            throw new ArgumentException("Invalid Operator");
        }

        private long LogicalShift(long lhs, int rhs)
        {
            var by = rhs;
            var value = lhs;
            var shifted = (value >> by);

            for (int b = by; b > 0; b--)
            {
                shifted = (shifted & ~(1 << ((31 - (b - 1)) % 32)));
            }

            return shifted;
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

        private void PrintAsBitSet(int value)
        {
            int maxOffset = 5;
            var val = value.ToString();
            string pad = " ";
            int len = val.Length;

            if (value >= 0)
            {
                pad += " ";
            }
            else
            {
                len--;
            }

            List<Run> runs = new List<Run>();

            string valueToPrint = pad + value.ToString() + new string(' ', Math.Abs(maxOffset - len)) + " => ";
            runs.Add(new Run() { Text = valueToPrint, Color = (RunColor)(int)Console.ForegroundColor });

            for (int b = 31; b >= 0; b--)
            {
                var isSet = (value & (1 << (b % 32))) != 0;
                if (isSet)
                {
                    runs.Add(new Run() { Text = "1", Color = RunColor.Green });
                }
                else
                {
                    runs.Add(new Run() { Text = "0", Color = RunColor.Blue });
                }

                if (b > 0 && b % 8 == 0)
                {
                    runs.Add(new Run() { Text = "_", Color = RunColor.White });
                }
            }

            printer.Print(runs.ToArray());
        }
    }
}
