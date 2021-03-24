using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiny.Calculator
{
    public class Definitions
    {
        public static char[] OperatorGlyphs = new char[] { '+', '-', '/', '*', '^', '%', '~', '|', '&', '>', '<' };
        public static string[] Operators    = new string[] { "+", "-", "/", "*", "^", "%", "~", "|", "&", ">>", "<<", ">>>" };
        public static string[] Commands     = new string[] { "cls", "parse", "explain", "explain_on", "explain_off", "code", "help", "regs", "vars", "mem" };
        public static string[] Instructions = new string[] { "label", "mov", "add", "sub", "mul", "div", "shr", "shl", "cmp", "jle", "jge", "jg", "jl", "je", "jne" };
    }
}
