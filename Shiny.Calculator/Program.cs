using Shiny.Calculator;
using Shiny.Calculator.Evaluation;
using Shiny.Repl.Parsing;
using Shiny.Repl.Tokenization;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Shiny.Repl
{
    class Program
    {
        private static char[] operators = new char[] { '+', '-', '/', '*', '^', '~', '|', '&', '>', '<' };
        private static string[] commands = new string[] { "cls", "explain", "help?" };

        static void Main(string[] args)
        {   
            var prompt = ">>> ";

            while (true)
            {
                var statement = ProcessKeyEvents(prompt);
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
                        statementBuilder.Insert(bufferIndex, keyInfo.KeyChar);
                    }

                    bufferIndex++;

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

                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    Console.Write(" ");
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);

                    if (bufferIndex >= statementBuilder.Length)
                    {
                        statementBuilder.Remove(statementBuilder.Length - 1, 1);
                    }
                    else
                    {
                        statementBuilder.Remove(bufferIndex - 1, 1);
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
                        statementBuilder.Insert(bufferIndex, keyInfo.KeyChar);
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
            Tokenizer tokenizer = new Tokenizer();
            Parser parser = new Parser();
            Evaluator evaluator = new Evaluator();
            VariableResolver resolver = new VariableResolver();
            ConsolePrinter printer = new ConsolePrinter();

            var tokens = tokenizer.Tokenize(statement);
            var ast = parser.Parse(tokens);

            Console.WriteLine();

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

            var result = evaluator.Evaluate(ast, variables, printer);
            return result;
        }
    }
}
