using Shiny.Calculator;
using Shiny.Calculator.Evaluation;
using Shiny.Repl.Parsing;
using Shiny.Repl.Tokenization;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Shiny.Repl
{


    class Program
    {
        private static char[] operators = new char[] { '+', '-', '/', '*', '^', '~', '|', '&', '>', '<' };
        private static string[] commands = new string[] { "cls", "explain", "explain_on", "explain_off", "help?" };
        private static string[] history = new string[64];
        private static int historyIndex = 0;
        private static string prompt = ">>> ";


        private static  EvaluatorContext context = new EvaluatorContext();
        private static  Tokenizer tokenizer = new Tokenizer();
        private static  Parser parser = new Parser(commands);
        private static  Evaluator evaluator = new Evaluator();
        private static  ConsolePrinter printer = new ConsolePrinter();

        static void Main(string[] args)
        {
            Console.WriteLine(PrintLogo());

            while (true)
            {
                var statement = ProcessKeyEvents(prompt);
                history[historyIndex++ % history.Length] = statement;
                Evaluate(statement, prompt);
            }
        }

        static string ProcessKeyEvents(string prompt)
        {
            Console.Write(prompt);
            StringBuilder statementBuilder = new StringBuilder();

            Console.ForegroundColor = ConsoleColor.Green;

            var keyInfo = new ConsoleKeyInfo();
            int bufferIndex = 0;
            int baseIndex = prompt.Length;

            while (keyInfo.Key != ConsoleKey.Enter)
            {
                keyInfo = Console.ReadKey(true);
                if (operators.Contains(keyInfo.KeyChar))
                {
                    Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(keyInfo.KeyChar);
                    Console.ForegroundColor = ConsoleColor.Green;

                    if (bufferIndex >= statementBuilder.Length)
                    {
                        statementBuilder.Append(keyInfo.KeyChar);
                    }
                    else
                    {
                        InsertBetween(statementBuilder, keyInfo.KeyChar, bufferIndex);
                    }

                    bufferIndex++;

                }
                else if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (historyIndex > 0) historyIndex--;
                    var historyStatement = history[historyIndex];

                    Console.SetCursorPosition(baseIndex, Console.CursorTop);
                    Console.Write(new string(' ', statementBuilder.Length));
                    Console.SetCursorPosition(baseIndex, Console.CursorTop);

                    Console.Write(historyStatement);
                    statementBuilder.Clear();
                    statementBuilder.Append(historyStatement);
                    bufferIndex = statementBuilder.Length;

                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    if (historyIndex < history.Length) historyIndex++;
                    var historyStatement = history[historyIndex & history.Length];

                    Console.SetCursorPosition(baseIndex, Console.CursorTop);
                    Console.Write(new string(' ', statementBuilder.Length));
                    Console.SetCursorPosition(baseIndex, Console.CursorTop);

                    Console.Write(historyStatement);
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
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);

                        statementBuilder.Remove(statementBuilder.Length - 1, 1);
                    }
                    else
                    {
                        RemoveBetween(statementBuilder, keyInfo.KeyChar, bufferIndex);
                    }

                    bufferIndex--;

                }
                else if (keyInfo.KeyChar == '\r')
                {
                    break;
                }
                else
                {
                    Console.Write(keyInfo.KeyChar);

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
                            ConsoleUtils.Write(ConsoleColor.Blue, command);
                        }
                    }

                }
            }

            return statementBuilder.ToString();
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
            Console.Write(rhs + " ");
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

            Console.Write(all.Substring(bufferIndex));
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

        private static EvaluatorState Evaluate(string statement, string prompt)
        {            
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(statement))
            {
                return null;
            }

            VariableResolver resolver = new VariableResolver();

            var tokens = tokenizer.Tokenize(statement);
            var ast = parser.Parse(tokens);

            var variables = resolver.Resolve(ast);

            foreach (var resolved in variables)
            {
                var nestedPrompt = $"{resolved.Key} = ";

                var stmt = ProcessKeyEvents(nestedPrompt);
                var value = Evaluate(stmt, nestedPrompt);

                printer.Print(new Run() { Text = "  --------", Color = RunColor.White });

                var existing = variables[resolved.Key];

                existing.IsSigned = value.IsSigned;
                existing.Type = value.Type;
                existing.Value = value.Value;
            }

            var result = evaluator.Evaluate(ast, variables, printer, context);
            return result;

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
    }
}
