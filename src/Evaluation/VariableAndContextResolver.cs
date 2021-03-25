using Shiny.Calculator.Parsing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using BinaryExpression = Shiny.Calculator.Parsing.BinaryExpression;
using AST_Node = Shiny.Calculator.Parsing.AST_Node;
using UnaryExpression = Shiny.Calculator.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public class ResolvedContext
    {
        public Dictionary<string, EvaluatorState> ResolvedVariables;
    }

    public class VariableAndContextResolver
    {
        private Enviroment enviroment = new Enviroment();

        private Dictionary<string, EvaluatorState> variables = new Dictionary<string, EvaluatorState>();

        public bool Resolve(AST syntaxTree, IPrinter printer, out ResolvedContext resolved)
        { 
            ResolveErrors(syntaxTree, printer);

            variables.Clear();

            foreach (var stmt in syntaxTree.Statements)
                Visit(stmt);

            resolved = new ResolvedContext();
            resolved.ResolvedVariables = variables;
                
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

                    if (error.Possition >= 0)
                    {
                        printer.Print(Run.Yellow(new string(' ', error.Possition) + marker));
                    }

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
            else if (expression is IdentifierExpression identifierExpression)
            {
                variables.TryAdd(identifierExpression.Identifier, new EvaluatorState());
                return;
            }
            else if (expression is BlockExpression block)
            {
                block.LabelToAddressMap = new Dictionary<string, int>();
                //
                // Block needs an index that will increment. 
                // so when we encounter a label we need to save that index.
                //
                int idx = 0;
                foreach (var stmt in block.Body)
                {
                    if (stmt is AST_Label label)
                    {
                        label.Address = idx.ToString();
                        block.LabelToAddressMap.Add(label.Label, idx);
                    }
                    else
                    {
                        Visit(stmt);
                    }
                    idx++;
                }
            }
            else if(expression is VariableAssigmentExpression variableAssigmentExpression)
            {
                Visit(variableAssigmentExpression.Assigment);
                return;
            }

            return;
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
