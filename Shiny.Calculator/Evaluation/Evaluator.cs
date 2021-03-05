using Shiny.Repl.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shiny.Calculator.Evaluation;

using BinaryExpression = Shiny.Repl.Parsing.BinaryExpression;
using AST_Node = Shiny.Repl.Parsing.AST_Node;
using UnaryExpression = Shiny.Repl.Parsing.UnaryExpression;
using System.IO;

namespace Shiny.Calculator.Evaluation
{
    public class EvaluatorContext
    {
        public bool IsExplainAll;
        public bool IsExplain;
    }

    public class Evaluator
    {
        private Dictionary<string, EvaluatorState> variables = null;
        private Dictionary<string, EvaluatorState> registers = new Dictionary<string, EvaluatorState>() 
        {
            { "eax", new EvaluatorState() { Value = "0", Type = LiteralType.Number } },
            { "ebx", new EvaluatorState() { Value = "0", Type = LiteralType.Number } },
            { "ecx", new EvaluatorState() { Value = "0", Type = LiteralType.Number } },
            { "edx", new EvaluatorState() { Value = "0", Type = LiteralType.Number } }
        };

        public EvaluatorContext context;
        private IPrinter printer;

        public EvaluatorState Evaluate(
            AST_Node expression,
            Dictionary<string, EvaluatorState> resolvedVariables,
            IPrinter printer, EvaluatorContext context)
        {
            this.context = context;
            this.printer = printer;
            variables = resolvedVariables;
            var result = Visit(expression);

            if (context.IsExplainAll) context.IsExplain = true;

            if (result.Value != null)
            {
                printer.Print(new Run() { Text = "    =", Color = RunColor.Red });
                PrintAsBitSet((int)long.Parse(result.Value));
            }

            return result;
        }

        private EvaluatorState Visit(AST_Node expression)
        {
            if (expression is BinaryExpression operatorExpression)
            {
                return EvaluateBinaryExpression(operatorExpression);
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                return EvaluateUnaryExpression(unaryExpression);
            }
            else if (expression is LiteralExpression literalExpression)
            {
                return EvaluateLiteralExpression(literalExpression);
            }
            else if (expression is IdentifierExpression identifierExpression)
            {
                return variables[identifierExpression.Identifier];
            }
            else if(expression is ASM_Instruction assemblyInstruction)
            {
                return EvaluateAssemblyInstruction(assemblyInstruction);
            }
            else if(expression is CommandExpression commandExpression)
            {
                if (commandExpression.CommandName == "explain")
                {
                    printer.Print(Run.White("Explain is ON"));

                    context.IsExplain = true;
                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "explain_on")
                {
                    printer.Print(Run.White("Explain is ON"));

                    context.IsExplainAll = true;
                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "explain_off")
                {
                    printer.Print(Run.White("Explain is OFF"));

                    context.IsExplainAll = false;
                    context.IsExplain = false;

                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "cls")
                {
                    printer.Clear();
                    return new EvaluatorState();
                }
                else if(commandExpression.CommandName == "code")
                {
                    //
                    // Should be file path;
                    //
                    var path = commandExpression.RightHandSide.ToString();
                    printer.Clear();

                    PrintCode(path.Replace("\"",string.Empty));

                    return new EvaluatorState();
                }
                else if(commandExpression.CommandName == "parse")
                {
                    var rhs = commandExpression.RightHandSide;
                    ASTPrinter ast = new ASTPrinter(printer);
                    ast.Print(rhs);

                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "regs")
                {
                    foreach(var register in registers)
                    {
                        printer.PrintInline(Run.White("    " + register.Key + " :"));
                        PrintAsBitSet((int)long.Parse(register.Value.Value));
                    }

                    return new EvaluatorState();
                }

                return Visit(commandExpression.RightHandSide);
            }

            throw new ArgumentException($"Invalid Expression: '{expression.ToString()}'");
        }

        private EvaluatorState EvaluateAssemblyInstruction(ASM_Instruction assemblyInstruction)
        {
            if(assemblyInstruction is BinaryASMInstruction binaryAsm)
            {
                if(binaryAsm.Name == "mov")
                {
                    EvaluateAsmMov(binaryAsm);
                }
                else if(binaryAsm.Name == "add")
                {
                    EvaluateAsmAdd(binaryAsm);
                }
                else if (binaryAsm.Name == "sum")
                {
                    EvaluateAsmAdd(binaryAsm);
                }
                else if (binaryAsm.Name == "shr")
                {
                    EvaluateAsmShr(binaryAsm);
                }
                else if (binaryAsm.Name == "shl")
                {
                    EvaluateAsmShl(binaryAsm);
                }
            }
            else if(assemblyInstruction is UnaryASMInstruction unaryASM)
            {
                if (unaryASM.Name == "mul")
                {
                    EvaluateAsmMul(unaryASM);
                }
                else if (unaryASM.Name == "div")
                {
                    EvaluateAsmDiv(unaryASM);
                }
            }

            return new EvaluatorState();
        }

        private void EvaluateAsmMov(BinaryASMInstruction mov)
        {
            EvaluatorState state = null;
            if (mov.Source is LiteralExpression literal)
            {
                state = EvaluateLiteralExpression(literal);
            }
            else if (mov.Source is IdentifierExpression identifier)
            {
                state = registers[identifier.Identifier];
            }

            if (mov.Desination is IdentifierExpression identifierDest)
            {
                registers[identifierDest.Identifier] = state;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = "    " + identifierDest.Identifier, Color = RunColor.White });
                    PrintAsBitSet((int)long.Parse(state.Value));
                }
            }
        }

        private void EvaluateAsmAdd(BinaryASMInstruction add)
        {
            EvaluatorState sourceState = null;
            if (add.Source is LiteralExpression literal)
            {
                sourceState = EvaluateLiteralExpression(literal);
            }
            else if (add.Source is IdentifierExpression identifier)
            {
                sourceState = registers[identifier.Identifier];
            }

            if (add.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (long.Parse(sourceState.Value) + long.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = "    " + identifierDest.Identifier, Color = RunColor.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
        }

        private void EvaluateAsmSub(BinaryASMInstruction sub)
        {
            EvaluatorState sourceState = null;
            if (sub.Source is LiteralExpression literal)
            {
                sourceState = EvaluateLiteralExpression(literal);
            }
            else if (sub.Source is IdentifierExpression identifier)
            {
                sourceState = registers[identifier.Identifier];
            }

            if (sub.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (long.Parse(sourceState.Value) - long.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = "    " + identifierDest.Identifier, Color = RunColor.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
        }

        private void EvaluateAsmShr(BinaryASMInstruction shr)
        {
            EvaluatorState sourceState = null;
            if (shr.Source is LiteralExpression literal)
            {
                sourceState = EvaluateLiteralExpression(literal);
            }
            else if (shr.Source is IdentifierExpression identifier)
            {
                sourceState = registers[identifier.Identifier];
            }

            if (shr.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (int.Parse(sourceState.Value) >> int.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = "    " + identifierDest.Identifier, Color = RunColor.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
        }

        private void EvaluateAsmMul(UnaryASMInstruction mul)
        {
            EvaluatorState sourceState = null;
            if (mul.Source is LiteralExpression literal)
            {
                sourceState = EvaluateLiteralExpression(literal);
            }
            else if (mul.Source is IdentifierExpression identifier)
            {
                sourceState = registers[identifier.Identifier];
            }

            var destState = registers["eax"];
            var result = (long.Parse(destState.Value) * long.Parse(sourceState.Value));

            destState.Value = result.ToString();
            registers["eax"] = destState;

            if (context.IsExplain)
            {
                printer.Print(new Run() { Text = "    " + "eax", Color = RunColor.White });
                PrintAsBitSet((int)long.Parse(destState.Value));
            }

        }

        private void EvaluateAsmDiv(UnaryASMInstruction div)
        {
            EvaluatorState sourceState = null;
            if (div.Source is LiteralExpression literal)
            {
                sourceState = EvaluateLiteralExpression(literal);
            }
            else if (div.Source is IdentifierExpression identifier)
            {
                sourceState = registers[identifier.Identifier];
            }

            var destState = registers["eax"];
            var result = (long.Parse(destState.Value) / long.Parse(sourceState.Value));

            destState.Value = result.ToString();
            registers["eax"] = destState;

            if (context.IsExplain)
            {
                printer.Print(new Run() { Text = "    " + "eax", Color = RunColor.White });
                PrintAsBitSet((int)long.Parse(destState.Value));
            }

        }

        private void EvaluateAsmShl(BinaryASMInstruction shl)
        {
            EvaluatorState sourceState = null;
            if (shl.Source is LiteralExpression literal)
            {
                sourceState = EvaluateLiteralExpression(literal);
            }
            else if (shl.Source is IdentifierExpression identifier)
            {
                sourceState = registers[identifier.Identifier];
            }

            if (shl.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (int.Parse(sourceState.Value) << int.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = "    " + identifierDest.Identifier, Color = RunColor.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
        }

 
        private void PrintCode(string path)
        {
            var source = File.ReadAllText(path);
            new Shiny.Calculator.Evaluation.CodeColorizer(printer).Colorize(source);
        }
    
        private EvaluatorState EvaluateLiteralExpression(LiteralExpression literalExpression)
        {
            return new EvaluatorState() 
            { 
                Type = literalExpression.Type, 
                IsSigned = literalExpression.IsSigned, 
                Value = literalExpression.Value 
            };
        }

        private EvaluatorState EvaluateUnaryExpression(UnaryExpression unaryExpression)
        {
            var @operator = unaryExpression.Operator;
            var lhs = Visit(unaryExpression.Left);

            var result = EvalUnaryAsNumber(@operator, lhs);

            if (context.IsExplain)
            {
                PrintAsBitSet((int)long.Parse(lhs.Value));
            }

            return new EvaluatorState() { IsSigned = true, Type = lhs.Type, Value = result.ToString() };
        }

        private EvaluatorState EvaluateBinaryExpression(BinaryExpression operatorExpression)
        {
            var @operator = operatorExpression.Operator;
            var lhs = Visit(operatorExpression.Left);
            var rhs = Visit(operatorExpression.Right);

            if (context.IsExplain)
            {
                PrintAsBitSet((int)long.Parse(lhs.Value));
                printer.Print(new Run() { Text = "    " + operatorExpression.Operator, Color = RunColor.Red });
                PrintAsBitSet((int)long.Parse(rhs.Value));
                printer.Print(new Run() { Text = "    " + "````````", Color = RunColor.White });
            }

            var result = EvalBinaryAsNumber(@operator, lhs, rhs);

            return new EvaluatorState() { IsSigned = lhs.IsSigned, Type = lhs.Type, Value = result.ToString() };
        }


        private long EvalBinaryAsNumber(string op, EvaluatorState lhs, EvaluatorState rhs)
        {
            switch (op)
            {
                case "+": return (long.Parse(lhs.Value) + long.Parse(rhs.Value));
                case "-": return (long.Parse(lhs.Value) - long.Parse(rhs.Value));
                case "*": return (long.Parse(lhs.Value) * long.Parse(rhs.Value));
                case "/": return (long.Parse(lhs.Value) / long.Parse(rhs.Value));

                case "&": return (long.Parse(lhs.Value) & long.Parse(rhs.Value));
                case "|": return (long.Parse(lhs.Value) | long.Parse(rhs.Value));
                case "^": return (long.Parse(lhs.Value) ^ long.Parse(rhs.Value));
                case "%": return (long.Parse(lhs.Value) % long.Parse(rhs.Value));
                case ">>": return (long.Parse(lhs.Value) >> int.Parse(rhs.Value));
                case "<<": return (long.Parse(lhs.Value) << int.Parse(rhs.Value));

                //Logical shift pad with zeros
                case ">>>": return LogicalShift(long.Parse(lhs.Value), int.Parse(rhs.Value));
            }

            throw new ArgumentException("Invalid Operator");
        }

        private long LogicalShift(long lhs, int rhs)
        {
            var by = rhs;
            var value = lhs;
            var shifted = (value >> by);

            for (int b = by; b > 0; b--)
            {
                shifted = (shifted & ~(1 << ((31 - (b - 1)) % 32)));
            }

            return shifted;
        }

        private long EvalUnaryAsNumber(string op, EvaluatorState lhs)
        {
            switch (op)
            {
                case "-": return -(long.Parse(lhs.Value));
                case "~": return ~(long.Parse(lhs.Value));
            }

            throw new ArgumentException("Invalid Operator");
        }

        private void PrintAsBitSet(int value)
        {
            int maxOffset = 5;
            var val = value.ToString();
            string pad = "   ";
            int len = val.Length;

            if (value >= 0)
            {
                pad += " ";
            }
            else
            {
                len--;
            }

            List<Run> runs = new List<Run>();

            string valueToPrint = pad + value.ToString() + new string(' ', Math.Abs(maxOffset - len)) + " => ";
            runs.Add(new Run() { Text = valueToPrint, Color = (RunColor)(int)Console.ForegroundColor });

            for (int b = 31; b >= 0; b--)
            {
                var isSet = (value & (1 << (b % 32))) != 0;
                if (isSet)
                {
                    runs.Add(new Run() { Text = "1", Color = RunColor.Green });
                }
                else
                {
                    runs.Add(new Run() { Text = "0", Color = RunColor.Blue });
                }

                if (b > 0 && b % 8 == 0)
                {
                    runs.Add(new Run() { Text = "_", Color = RunColor.White });
                }
            }

            printer.Print(runs.ToArray());
        }
    }
}
