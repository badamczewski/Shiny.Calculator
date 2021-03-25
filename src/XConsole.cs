using System;
using System.Collections.Generic;
using System.Text;

namespace Shiny.Calculator
{
    //
    // ASCII console with multi-color support and a couple of other features.
    //
    public class XConsole
    {
        private const string ESC = "\x1b";

        public static Color ForegroundColor { get; set; }

        static XConsole()
        {
            ForegroundColor = Colors.White;
        }

        public static void Write(string text, Color color)
        {
            Console.Write($"{GetColorForegroundString(color)}{text}{GetColorForegroundString(ForegroundColor)}");
        }

        public static void WriteLine(string text, Color color)
        {
            Console.WriteLine($"{GetColorForegroundString(color)}{text}{GetColorForegroundString(ForegroundColor)}");
        }

        public static string GetColorForegroundString(Color color)
        {
            return string.Concat(ESC, "[38;2;", color.R, ";", color.G, ";", color.B, "m");
        }

        public static void Write(string text)     => Write(text, ForegroundColor);
        public static void WriteLine(string text) => WriteLine(text, ForegroundColor);
        public static void WriteLine()            => Console.WriteLine();
        public static void Clear()                => Console.Clear();
    }

    public class Colors
    {
        public static Color Green  = new Color(0, 176, 80);
        public static Color Yellow = new Color(255, 255, 0);
        public static Color Blue   = new Color(86, 156, 214);
        public static Color Red    = new Color(255, 0, 0);
        public static Color White  = new Color(255, 255, 255);
        public static Color Gray   = new Color(192, 192, 192);
        public static Color DarkGray = new Color(128, 128, 128);
    }

    public struct Color
    {
        public int R;
        public int G;
        public int B;

        public Color(int r, int g, int b)
        {
            R = r; G = g; B = b;
        }

        public void Deconstruct(out int r, out int g, out int b)
        {
            r = R;
            g = G;
            b = B;
        }
    }
}
