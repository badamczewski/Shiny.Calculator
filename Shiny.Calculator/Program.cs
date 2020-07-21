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
        private static char[] operators = new char[] { '+', '-', '/', '*', '^', '~', '|', '&' };
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
                }
            }

            return statementBuilder.ToString();
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
