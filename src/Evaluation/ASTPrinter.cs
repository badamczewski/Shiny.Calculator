using Shiny.Calculator.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiny.Calculator.Evaluation
{
    public class ASTPrinter
    {
        private IPrinter _printer;

        public ASTPrinter(IPrinter printer)
        {
            _printer = printer;
        }

        public void Print(AST_Node expression)
        {
            Visit(expression);
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
                EvaluateIdentifierExpression(identifierExpression);
                return;
            }

            _printer.Print(Run.Yellow($"???-{expression.ID}"));
        }

        private EvaluatorState EvaluateIdentifierExpression(IdentifierExpression identifierExpression)
        {
            _printer.Print(
                Run.Green($"IDENTIFIER-{identifierExpression.ID} =>"),
                Run.White($"'{identifierExpression.Identifier}'"));

            return null;
        }

        private EvaluatorState EvaluateLiteralExpression(LiteralExpression literalExpression)
        {
            _printer.Print(
                Run.Green($"LITERAL-{literalExpression.ID} =>"),
                Run.White($"'{literalExpression.Value}'"));

            return null;
        }

        private void EvaluateUnaryExpression(UnaryExpression unaryExpression)
        {
            _printer.Print(
                Run.Green($"UNARY-{unaryExpression.ID} =>"));

            _printer.Indent += 4;

            _printer.Print(
                Run.Red($"OPERATOR = {unaryExpression.Operator}"));

            Visit(unaryExpression.Left);

            _printer.Indent -= 4;
        }

        private void EvaluateBinaryExpression(BinaryExpression operatorExpression)
        {
            _printer.Print(
                Run.Green($"BINARY-{operatorExpression.ID} =>"));

            
            _printer.Indent += 4;

            _printer.Print(
                Run.Red($"OPERATOR = {operatorExpression.Operator}"));

            Visit(operatorExpression.Left);
            Visit(operatorExpression.Right);

            _printer.Indent -= 4;
        }
    }
}
