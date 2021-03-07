using Shiny.Repl.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using AST_Node = Shiny.Repl.Parsing.AST_Node;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public class VariableResolver
    {
        private Dictionary<string, EvaluatorState> variables = new Dictionary<string, EvaluatorState>();
        public Dictionary<string, EvaluatorState> Resolve(AST_Node expression)
        {
            variables.Clear();
            Visit(expression);
            return variables;
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
                variables.TryAdd(identifierExpression.Identifier, new EvaluatorState() { IsResolved = false });
                return;
            }
            else if(expression is VariableAssigmentExpression variableAssigmentExpression)
            {
                return;
            }
            else if (expression is CommandExpression commandExpression)
            {
                return;
            }
            else if (expression is ASM_Instruction asm)
            {
                return;
            }
            else if(expression is AST_Error error)
            {
                return;
            }

            throw new ArgumentException($"Invalid Expression: '{expression.ToString()}'");
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
