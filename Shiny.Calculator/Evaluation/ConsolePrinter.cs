using System;
using System.Collections.Generic;
using System.Text;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using Expression = Shiny.Repl.Parsing.AST_Node;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public enum RunColor
    {
        //
        // Summary:
        //     The color black.
        Black = 0,
        //
        // Summary:
        //     The color dark blue.
        DarkBlue = 1,
        //
        // Summary:
        //     The color dark green.
        DarkGreen = 2,
        //
        // Summary:
        //     The color dark cyan (dark blue-green).
        DarkCyan = 3,
        //
        // Summary:
        //     The color dark red.
        DarkRed = 4,
        //
        // Summary:
        //     The color dark magenta (dark purplish-red).
        DarkMagenta = 5,
        //
        // Summary:
        //     The color dark yellow (ochre).
        DarkYellow = 6,
        //
        // Summary:
        //     The color gray.
        Gray = 7,
        //
        // Summary:
        //     The color dark gray.
        DarkGray = 8,
        //
        // Summary:
        //     The color blue.
        Blue = 9,
        //
        // Summary:
        //     The color green.
        Green = 10,
        //
        // Summary:
        //     The color cyan (blue-green).
        Cyan = 11,
        //
        // Summary:
        //     The color red.
        Red = 12,
        //
        // Summary:
        //     The color magenta (purplish-red).
        Magenta = 13,
        //
        // Summary:
        //     The color yellow.
        Yellow = 14,
        //
        // Summary:
        //     The color white.
        White = 15
    }

    public class Run
    {
        public string Text;
        public RunColor Color;

        public static Run Green(string text) 
            => new Run() { Text = text, Color = RunColor.Green };

        public static Run White(string text)
           => new Run() { Text = text, Color = RunColor.White };

        public static Run Red(string text)
            => new Run() { Text = text, Color = RunColor.Red };

        public static Run Blue(string text)
            => new Run() { Text = text, Color = RunColor.Blue };
    }

    public class ConsolePrinter : IPrinter
    {
        public void Clear()
        {
            Console.Clear();
        }

        public void PrintInline(params Run[] runs)
        {
            if (runs != null)
            {
                Console.SetCursorPosition(Console.CursorLeft + Indent, Console.CursorTop);

                foreach (var run in runs)
                {
                    var copy = Console.ForegroundColor;
                    Console.ForegroundColor = (ConsoleColor)(int)run.Color;
                    Console.Write(run.Text);
                    Console.ForegroundColor = copy;
                }
            }
        }

        public void Print(params Run[] runs)
        {
            PrintInline(runs);
            Console.WriteLine();
        }

        public int Indent { get; set; }
    }
}
