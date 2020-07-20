using System;
using System.Collections.Generic;
using System.Text;

namespace Shiny.Calculator
{
    public class ConsoleUtils
    {
        public static void Write(ConsoleColor color, string text)
        {
            var backupColor = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = backupColor;
        }

        public static void WriteLine(ConsoleColor color, string text)
        {
            var backupColor = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = backupColor;
        }
    }
}
