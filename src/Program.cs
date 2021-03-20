using Shiny.Calculator;
using Shiny.Calculator.Evaluation;
using Shiny.Calculator.Parsing;
using Shiny.Calculator.Tokenization;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shiny.Calculator
{
    class Program
    {
        private static char[] operatorGlyphs = new char[]   { '+', '-', '/', '*', '^', '%', '~', '|', '&', '>', '<' };
        private static string[] operators    = new string[] { "+", "-", "/", "*", "^", "%", "~", "|", "&", ">>", "<<", ">>>" };
        private static string[] commands     = new string[] { "cls", "parse", "explain", "explain_on", "explain_off", "code", "help", "regs", "vars", "mem", "result" };
        private static string[] instructions = new string[] { "mov", "add", "sub", "mul", "div", "shr", "shl" };
        private static string[] history      = new string[64];
        private static int historyIndex = 0;
        private static string prompt = ">>> ";

        private static EvaluatorContext context = new EvaluatorContext();
        private static Tokenizer tokenizer = new Tokenizer();
        private static Parser parser = new Parser(commands, operators, instructions);
        private static Evaluator evaluator = new Evaluator();
        private static ConsolePrinter printer = new ConsolePrinter();

        static void IntializeConsole()
        {
            XConsole.ForegroundColor = Colors.Green;
            XConsole.Clear();
            XConsole.WriteLine(PrintLogo());
        }

        static void Main(string[] args)
        {
            IntializeConsole();

            bool isMultiline = false;
            char breakKey = '\r';
            StringBuilder statementsBuilder = new StringBuilder();

            string currentPrompt = prompt;

            while (true)
            {
                var data = ProcessKeyEvents(currentPrompt, breakKey, isMultiline);
                statementsBuilder.Append(data.Statement);

                if (isMultiline)
                {
                    if (data.IsTerminated)
                    {
                        currentPrompt = prompt;
                        breakKey = '\r';
                        isMultiline = false;

                        Evaluate(statementsBuilder.ToString(), prompt);
                        statementsBuilder.Clear();
                    }
                }
                else
                {
                    var stmt = statementsBuilder.ToString();
                    history[historyIndex++ % history.Length] = stmt;
                    var result = Evaluate(stmt, prompt);

                    //
                    // Process special commands like multiline prompt.
                    //
                    if (result != null && result.Type == LiteralType.Special)
                    {
                        if (result.Value == EvaluatorSpecialState.MultiLineMode)
                        {
                            isMultiline = true;
                            breakKey = '}';
                            currentPrompt = ">>| ";
                        }
                    }
                    else
                    {
                        statementsBuilder.Clear();
                    }
                }
            }
        }

        static void ColorizeExpression(string expression)
        {
            foreach(var keyChar in expression)
            {
                Colorize(keyChar);
            }
        }

        static void Colorize(char keyChar)
        {
            if (operatorGlyphs.Contains(keyChar))
            {
                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop);
                XConsole.Write(keyChar.ToString(), Colors.Red);
            }
            else if (char.IsLetter(keyChar))
            {
                XConsole.Write(keyChar.ToString(), Colors.Yellow);
            }
            else
            {
                XConsole.Write(keyChar.ToString(), Colors.Green);
            }
        }

        static KeyBuffer ProcessKeyEvents(string prompt, char breakKey, bool isMultiLine = false)
        {
            XConsole.Write(prompt);
            StringBuilder statementBuilder = new StringBuilder();
            XConsole.ForegroundColor = Colors.Green;

            var keyInfo = new ConsoleKeyInfo();
            int bufferIndex = 0;
            int baseIndex = prompt.Length;
            bool isTerminated = false;

            while (keyInfo.KeyChar != breakKey)
            {
                keyInfo = Console.ReadKey(true);
                            
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (historyIndex > 0) historyIndex--;
                    var historyStatement = history[historyIndex];

                    if (historyStatement != null)
                    {
                        Console.SetCursorPosition(baseIndex, Console.CursorTop);
                        XConsole.Write(new string(' ', statementBuilder.Length + historyStatement.Length));
                        Console.SetCursorPosition(baseIndex, Console.CursorTop);

                        if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
                        {
                            XConsole.Write(statementBuilder.ToString() + historyStatement);
                            statementBuilder.Append(statementBuilder.ToString() + historyStatement);
                        }
                        else
                        {
                            //
                            // The statement needs to be colorized.
                            //
                            ColorizeExpression(historyStatement);
                            statementBuilder.Clear();
                            statementBuilder.Append(historyStatement);
                        }
                    }

                    bufferIndex = statementBuilder.Length;

                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    if (historyIndex < history.Length) historyIndex++;
                    var historyStatement = history[historyIndex & history.Length];

                    Console.SetCursorPosition(baseIndex, Console.CursorTop);
                    XConsole.Write(new string(' ', statementBuilder.Length));
                    Console.SetCursorPosition(baseIndex, Console.CursorTop);

                    ColorizeExpression(historyStatement);
                    statementBuilder.Clear();
                    statementBuilder.Append(historyStatement);
                    bufferIndex = statementBuilder.Length;
                }
                else if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    if (bufferIndex == 0)
                        continue;

                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    bufferIndex--;

                }
                else if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    if (bufferIndex >= statementBuilder.Length)
                        continue;

                    Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    bufferIndex++;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (bufferIndex == 0)
                        continue;

                    if (bufferIndex >= statementBuilder.Length)
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        XConsole.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);

                        statementBuilder.Remove(statementBuilder.Length - 1, 1);
                    }
                    else
                    {
                        RemoveBetween(statementBuilder, keyInfo.KeyChar, bufferIndex);
                    }

                    bufferIndex--;

                }
                else if(keyInfo.KeyChar == breakKey)
                {
                    //
                    // This is not an empty key so we need to display it.
                    //
                    if (keyInfo.Key != ConsoleKey.Enter)
                    {
                        statementBuilder.Append(keyInfo.KeyChar);
                        XConsole.Write(keyInfo.KeyChar.ToString());
                    }

                    isTerminated = true;
                    break;
                }
                //
                // Move to new line and break here.
                //
                else if (isMultiLine && keyInfo.Key == ConsoleKey.Enter)
                {
                    XConsole.WriteLine();
                    break;
                }
                else
                {
                    Colorize(keyInfo.KeyChar);

                    if (bufferIndex >= statementBuilder.Length)
                    {
                        statementBuilder.Append(keyInfo.KeyChar);
                    }
                    else
                    {
                        InsertBetween(statementBuilder, keyInfo.KeyChar, bufferIndex);
                    }

                    bufferIndex++;

                    foreach (var command in commands)
                    {
                        var clsIdx = IndexOf(statementBuilder, command);

                        if (clsIdx >= 0 && bufferIndex <= clsIdx + command.Length)
                        {
                            Console.SetCursorPosition(prompt.Length + clsIdx, Console.CursorTop);
                            XConsole.Write(command, Colors.Blue);
                        }
                    }

                }
            }

            return new KeyBuffer() { Statement = statementBuilder.ToString(), IsTerminated = isTerminated };
        }

        private static void RemoveBetween(StringBuilder statementBuilder, char key, int bufferIndex)
        {
            var all = statementBuilder.ToString();
            var lhs = all.Substring(0, bufferIndex - 1);
            var rhs = all.Substring(bufferIndex);

            statementBuilder.Clear();
            statementBuilder.Append(lhs);
            statementBuilder.Append(rhs);

            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            XConsole.Write(rhs + " ");
            Console.SetCursorPosition(prompt.Length + bufferIndex - 1, Console.CursorTop);
        }

        private static void InsertBetween(StringBuilder statementBuilder, char key, int bufferIndex)
        {
            //
            // Let's move all of the right hand characters.
            //
            var all = statementBuilder.ToString();
            var lhs = all.Substring(0, bufferIndex);
            var rhs = key + all.Substring(bufferIndex);

            statementBuilder.Clear();
            statementBuilder.Append(lhs);
            statementBuilder.Append(rhs);

            XConsole.Write(all.Substring(bufferIndex));
            Console.SetCursorPosition(prompt.Length + bufferIndex + 1, Console.CursorTop);
        }

        private static int IndexOf(StringBuilder stringBuilder, string value)
        {
            int matched = 0;
            int foundIdx = 0;
            for (int i = 0; i < stringBuilder.Length; i++)
            {
                if (stringBuilder[i] == value[matched])
                {
                    matched++;
                    if (matched >= value.Length)
                    {
                        return foundIdx;
                    }
                }
                else
                {
                    foundIdx = i;
                    matched = 0;
                }
            }

            return -1;
        }

        private static EvaluatorState Evaluate(string statement, string currentPrompt)
        {
            printer.Indent = prompt.Length;

            XConsole.WriteLine();

            if (string.IsNullOrWhiteSpace(statement))
            {
                return null;
            }

            VariableAndContextResolver resolver = new VariableAndContextResolver();

            var tokens = tokenizer.Tokenize(statement);
            var ast = parser.Parse(tokens);

            if (resolver.Resolve(ast, printer, out var variables))
            {
                var vars = evaluator.GetVariables();

                foreach (var resolved in variables)
                {
                    //
                    // Check if we have already resolved this variable.
                    // If we did then don't ask for value, we are good.
                    //
                    if (vars.ContainsKey(resolved.Key))
                        continue;

                    var nestedPrompt = $">>  {resolved.Key} = ";

                    var stmt = ProcessKeyEvents(nestedPrompt, '\r');
                    var value = Evaluate(stmt.Statement, nestedPrompt);

                    printer.Print(new Run() { Text = "--------", Color = Colors.White });
                    var existing = variables[resolved.Key];

                    if (value == null)
                    {
                        printer.Print(Run.Red($"Variable '{resolved.Key}' needs a value"));
                        return new EvaluatorState();
                    }

                    existing.IsSigned = value.IsSigned;
                    existing.Type = value.Type;
                    existing.Value = value.Value;
                }

                var result = evaluator.Evaluate(ast, variables, printer, context);
                return result;
            }

            return new EvaluatorState();
        }

        private static string PrintLogo()
        {
            return @"
  ___ _    _             ___      _         _      _           
 / __| |_ (_)_ _ _  _   / __|__ _| |__ _  _| |__ _| |_ ___ _ _ 
 \__ \ ' \| | ' \ || | | (__/ _` | / _| || | / _` |  _/ _ \ '_|
 |___/_||_|_|_||_\_, |  \___\__,_|_\__|\_,_|_\__,_|\__\___/_|  
                 |__/                                          ";
        }

        public class KeyBuffer
        {
            public string Statement { get; set; }
            public bool IsTerminated { get; set; }
        }
    }
}
