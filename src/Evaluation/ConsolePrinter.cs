using System;
using System.Collections.Generic;
using System.Text;

using BinaryExpression = Shiny.Calculator.Parsing.BinaryExpression;
using Expression = Shiny.Calculator.Parsing.AST_Node;
using UnaryExpression = Shiny.Calculator.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public class Run
    {
        public string Text;
        public Color Color;

        public static Run Green(string text) 
            => new Run() { Text = text, Color = Colors.Green };

        public static Run White(string text)
           => new Run() { Text = text, Color = Colors.White };

        public static Run Red(string text)
            => new Run() { Text = text, Color = Colors.Red };

        public static Run Blue(string text)
            => new Run() { Text = text, Color = Colors.Blue };

        public static Run Yellow(string text)
            => new Run() { Text = text, Color = Colors.Yellow };
    }

    public class NullPrinter : IPrinter
    {
        public int Indent { get => 0; set => value = 0; }

        public void Clear()
        {
        }

        public void Print(params Run[] runs)
        {
        }

        public void PrintInline(params Run[] runs)
        {
        }
    }

    public class ConsolePrinter : IPrinter
    {
        public void Clear()
        {
            XConsole.Clear();
        }

        public void PrintInline(params Run[] runs)
        {
            if (runs != null)
            {
                Console.SetCursorPosition(Console.CursorLeft + Indent, Console.CursorTop);

                foreach (var run in runs)
                {
                    var copy = XConsole.ForegroundColor;
                    XConsole.ForegroundColor = run.Color;
                    XConsole.Write(run.Text);
                    XConsole.ForegroundColor = copy;
                }
            }
        }

        public void Print(params Run[] runs)
        {
            PrintInline(runs);
            XConsole.WriteLine();
        }

        public int Indent { get; set; }
    }
}
