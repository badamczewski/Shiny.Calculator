﻿using Shiny.Repl.Parsing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using AST_Node = Shiny.Repl.Parsing.AST_Node;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;
using Shiny.Calculator.Parsing;
using Shiny.Repl.Tokenization;

namespace Shiny.Calculator.Evaluation
{
    public class VariableAndContextResolver
    {
        private Dictionary<string, EvaluatorState> variables = new Dictionary<string, EvaluatorState>();

        public bool Resolve(AST syntaxTree, IPrinter printer, out Dictionary<string, EvaluatorState> resolvedVariables)
        { 
            ResolveErrors(syntaxTree, printer);

            variables.Clear();
            Visit(syntaxTree.Root);
            resolvedVariables = variables;
            //
            // If the syntax tree is error free we can continue with evaluation.
            //
            var hasErrors = syntaxTree.Errors.Any();
            return !hasErrors;
        }

        private void ResolveErrors(AST syntaxTree, IPrinter printer)
        {
            //
            // Resolve parser errors early.
            //
            if (syntaxTree.Errors.Any())
            {
                List<Run> tokenRuns = new List<Run>();

                string marker = "^";
                foreach (var error in syntaxTree.Errors)
                {
                    tokenRuns.Clear();

                    printer.Print();

                    printer.Print(Run.Red($"Error: {error.ErrorMessage}"));
                    printer.Print();

                    if (error.SurroundingTokens != null)
                    {
                        int prevLength = 0;
                        foreach (var token in error.SurroundingTokens)
                        {
                            //
                            // Since our lexer/tokenizer discards withespaces, we can
                            // compute where each token starts and fill the rest with empty
                            // space.
                            //
                            var pad = new string(' ', token.Position - prevLength);

                            //
                            // Calculate the marker size based on the faulty token length.
                            //
                            if (token.Position == error.Possition)
                            {
                                marker = new string('^', token.GetValue().Length);
                                tokenRuns.Add(Run.Yellow(pad + token.GetValue()));
                            }
                            else
                            {
                                tokenRuns.Add(Run.White(pad + token.GetValue()));
                            }

                            prevLength = token.Position + token.GetValue().Length;
                        }
                    }

                    printer.PrintInline(tokenRuns.ToArray());

                    printer.Print();
                    printer.Print(Run.Yellow(new string(' ', error.Possition) + marker));

                    if (string.IsNullOrWhiteSpace(error.HelpMessage) == false)
                    {
                        printer.Print(Run.Yellow($"Hint: {error.HelpMessage}"));
                    }

                    printer.Print(Run.Yellow($"@ line:{error.Line} pos:{error.Possition}"));
                }
            }
        }

        private void Visit(AST_Node expression)
        {
            if (expression is BinaryExpression operatorExpression)
            {
                EvaluateBinaryExpression(operatorExpression);
                return;
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                EvaluateUnaryExpression(unaryExpression);
                return;
            }
            else if (expression is LiteralExpression literalExpression)
            {
                EvaluateLiteralExpression(literalExpression);
                return;
            }
            else if (expression is IdentifierExpression identifierExpression)
            {
                variables.TryAdd(identifierExpression.Identifier, new EvaluatorState());
                return;
            }

            return;
        }

        private EvaluatorState EvaluateLiteralExpression(LiteralExpression literalExpression)
        {
            return null;
        }

        private void EvaluateUnaryExpression(UnaryExpression unaryExpression)
        {
            Visit(unaryExpression.Left);
        }

        private void EvaluateBinaryExpression(BinaryExpression operatorExpression)
        {
            Visit(operatorExpression.Left);
            Visit(operatorExpression.Right);
        }
    }
}
