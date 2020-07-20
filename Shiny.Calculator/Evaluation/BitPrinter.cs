using System;
using System.Collections.Generic;
using System.Text;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using Expression = Shiny.Repl.Parsing.Expression;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public class BitPrinter : IPrinter
    {
        int maxOffset = 5;

        public void PrintEquals(EvaluatorState state)
        {
            Console.WriteLine("  =");
            PrintAsBitSet((int)long.Parse(state.Value));
        }

        public void Print(Expression expression, EvaluatorState state)
        {
            PrintAsBitSet((int)long.Parse(state.Value));
        }
        public void PrintBinary(BinaryExpression binaryExpression, EvaluatorState left, EvaluatorState right)
        {
            PrintAsBitSet((int)long.Parse(left.Value));
            Console.WriteLine("  " + binaryExpression.Operator);
            PrintAsBitSet((int)long.Parse(right.Value));
        }
        public void PrintUnary(UnaryExpression unaryExpression, EvaluatorState left)
        {
            PrintAsBitSet((int)long.Parse(left.Value));
        }

        private void PrintAsBitSet(int value)
        {
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

            Console.Write(pad + value.ToString() + new string(' ', Math.Abs(maxOffset - len)) + " => ");

            for (int b = 31; b >= 0; b--)
            {
                var isSet = (value & (1 << (b % 32))) != 0;
                if (isSet)
                {
                    ConsoleUtils.Write(ConsoleColor.Green, "1");
                }
                else
                {
                    ConsoleUtils.Write(ConsoleColor.Blue, "0");
                }

                if (b > 0 && b % 8 == 0)
                {
                    ConsoleUtils.Write(ConsoleColor.White, "_");
                }
            }

            Console.WriteLine();
        }
    }
}
