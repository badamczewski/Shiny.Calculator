using Shiny.Calculator.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shiny.Calculator.Evaluation;

using BinaryExpression = Shiny.Calculator.Parsing.BinaryExpression;
using AST_Node = Shiny.Calculator.Parsing.AST_Node;
using UnaryExpression = Shiny.Calculator.Parsing.UnaryExpression;
using System.IO;

namespace Shiny.Calculator.Evaluation
{
    public class EvaluatorContext
    {
        public bool IsExplainAll;
        public bool IsExplain;
        public bool IsAssemblyContext;
    }

    public class Evaluator
    {
        private Enviroment enviroment = new Enviroment();
        private Heap heap = new Heap(256);

        private Dictionary<string, EvaluatorState> registers = new Dictionary<string, EvaluatorState>() 
        {
            { "eax", new EvaluatorState() { Value = "0", Type = LiteralType.Number } },
            { "ebx", new EvaluatorState() { Value = "0", Type = LiteralType.Number } },
            { "ecx", new EvaluatorState() { Value = "0", Type = LiteralType.Number } },
            { "edx", new EvaluatorState() { Value = "0", Type = LiteralType.Number } }
        };
        private Dictionary<string, int> flags = new Dictionary<string, int>()
        {
            { "OF", 0 },
            { "DF", 0 },
            { "IF", 0 },
            { "TF", 0 },
            { "SF", 0 },
            { "ZF", 0 },
            { "AF", 0 },
            { "PF", 0 },
            { "CF", 0 },
        };

        private EvaluatorContext context;
        private IPrinter printer;

        public EvaluatorState Evaluate(
            AST syntaxTree,
            ResolvedContext resolved,
            IPrinter printer, EvaluatorContext context)
        {
            this.context = context;
            this.printer = printer;
            
            foreach (var resolvedVariable in resolved.ResolvedVariables)
            {
                if(enviroment.Variables.TryGetValue(resolvedVariable.Key, out var varialbe) == false)
                {
                    enviroment.Variables.Add(resolvedVariable.Key, resolvedVariable.Value);
                }
            }

            EvaluatorState result = null;
            foreach (var stmt in syntaxTree.Statements)
            {
                result = Visit(stmt);

                if (context.IsExplainAll) context.IsExplain = true;

                if (result.Type == LiteralType.Number && result.Value != null)
                {
                    printer.Print(new Run() { Text = "=", Color = Colors.Red });
                    PrintAsBitSet((int)long.Parse(result.Value));
                }
                else if (result.Type == LiteralType.Text && result.Value != null)
                {
                    printer.Print(new Run() { Text = "=", Color = Colors.Red });
                    PrintAsText(result.Value);

                    var resultAsNumber = ConvertToNumber(result);

                    PrintAsBitSet((int)long.Parse(resultAsNumber.Value));
                }

                enviroment.Variables["res"] = result;
                enviroment.StatementIndex++;
            }

            return result;
        }

        public Dictionary<string, EvaluatorState> GetVariables()
        {
            return enviroment.Variables;
        }

        private EvaluatorState VisitAssemblyInstruction(AST_Node expression)
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
                if (context.IsAssemblyContext)
                    return registers[identifierExpression.Identifier];

                return enviroment.Variables[identifierExpression.Identifier];
            }

            throw new ArgumentException($"Invalid Instruction Expression: '{expression.ToString()}'");
        }

        private EvaluatorState Visit(AST_Node expression)
        {
            //
            // We either have a tree or a singe AST_Error node here.
            // So we need to process the error.
            //
            if(expression is AST_Error error)
            {
                printer.Print(Run.Red($" L:{error.Line} P:{error.Possition} {error.ErrorMessage}"));
                return new EvaluatorState();
            }
            else if (expression is AST_Label label)
            {
                return EvaluatorState.Empty();
            }

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
                return EvaluateIdentifierExpression(identifierExpression);
            }
            else if(expression is VariableAssigmentExpression assigmentExpression)
            {
                return EvaluateVariableAssigmentExpression(assigmentExpression);
            }
            else if(expression is ASM_Instruction assemblyInstruction)
            {
                return EvaluateAssemblyInstruction(assemblyInstruction);
            }
            else if(expression is BlockExpression blockExpression)
            {
                return EvaluateBlock(blockExpression);
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
                else if (commandExpression.CommandName == "code")
                {
                    //
                    // Should be file path;
                    //
                    var path = commandExpression.RightHandSide.ToString();
                    printer.Clear();

                    PrintCode(path.Replace("\"", string.Empty));

                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "parse")
                {
                    var rhs = commandExpression.RightHandSide;
                    ASTPrinter ast = new ASTPrinter(printer);
                    ast.Print(rhs);

                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "regs")
                {
                    foreach (var register in registers)
                    {
                        printer.PrintInline(Run.White(register.Key + " :"));
                        PrintAsBitSet((int)long.Parse(register.Value.Value));
                    }

                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "vars")
                {
                    int maxVarName = 8;
                    foreach (var variable in enviroment.Variables)
                    {
                        printer.PrintInline(
                            Run.White(variable.Key + new string(' ', Math.Abs(maxVarName - variable.Key.Length)) + " :"));

                        switch (variable.Value.Type)
                        {
                            case LiteralType.Number:
                            case LiteralType.Bool:
                            case LiteralType.Text:
                                PrintAsBitSet((int)long.Parse(variable.Value.Value));
                                break;
                            case LiteralType.Block:
                                PrintAsBlock();
                                break;
                        }
                    }

                    return new EvaluatorState();
                }
                else if (commandExpression.CommandName == "mem")
                {
                    var size = heap.Memory.Length / 4;
                    for (int i = 0; i < size; i++)
                    {
                        var value = heap.Read(i * 4);

                        printer.PrintInline(Run.White(i + " :"));
                        PrintAsBitSet(value);
                    }

                    return new EvaluatorState();
                }

                return Visit(commandExpression.RightHandSide);
            }

            throw new ArgumentException($"Invalid Expression: '{expression.ToString()}'");
        }

        private EvaluatorState EvaluateIdentifierExpression(IdentifierExpression identifierExpression)
        {
            if (context.IsAssemblyContext)
                return registers[identifierExpression.Identifier];

            var variable = enviroment.Variables[identifierExpression.Identifier];

            if(variable.Type == LiteralType.Block)
            {
                var block = (BlockState)variable;
                var envCopy = this.enviroment;
                this.enviroment = block.Enviroment;
                {
                    EvaluateBlock(block.Block);
                }
                this.enviroment = envCopy;
            }

            return variable;
        }

        private EvaluatorState EvaluateBlock(BlockExpression block)
        {            
            enviroment.StatementIndex = 0;
            //
            // A block needs it's own program counter
            //
            for(; enviroment.StatementIndex < block.Body.Count; enviroment.StatementIndex++)
            {
                var stmt = block.Body[enviroment.StatementIndex];
                Visit(stmt);
            }

            return new EvaluatorState();
        }

        private EvaluatorState EvaluateVariableAssigmentExpression(VariableAssigmentExpression assigmentExpression)
        {
            var identifier = assigmentExpression.Identifier.Identifier;

            if (assigmentExpression.Assigment is BlockExpression blockExpression)
            {
                return EvaluateBlockAssigmentExpression(identifier, blockExpression);
            }

            if (enviroment.Variables.TryGetValue(identifier, out var state))
            {
                state = Visit(assigmentExpression.Assigment);
                enviroment.Variables[identifier] = state;
            }
            else
            {
                state = Visit(assigmentExpression.Assigment);
                enviroment.Variables.Add(identifier, state);
            }
            return state;
        }

        private EvaluatorState EvaluateBlockAssigmentExpression(string identifier, BlockExpression blockExpression)
        {
            if (enviroment.Variables.TryGetValue(identifier, out var state))
            {
                state = new BlockState() {
                    Type = LiteralType.Block,
                    Block = blockExpression,
                    Enviroment = new Enviroment()
                    {
                        Labels = blockExpression.LabelToAddressMap
                    }
                };

                enviroment.Variables[identifier] = state;
            }
            else
            {
                state = new BlockState() {
                    Type = LiteralType.Block,
                    Block = blockExpression,
                    Enviroment = new Enviroment()
                    {
                        Labels = blockExpression.LabelToAddressMap
                    }
                };

                enviroment.Variables.Add(identifier, state);
            }

            return state;
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
                else if(binaryAsm.Name == "cmp")
                {
                    EvaluateAsmCmp(binaryAsm);
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
                else if (unaryASM.Name == "jle")
                {
                    EvaluateAsmJLE(unaryASM);
                }
                else if (unaryASM.Name == "jge")
                {
                    EvaluateAsmJGE(unaryASM);
                }
            }

            return new EvaluatorState();
        }

        private EvaluatorState EvaluateSource(AST_Node source, bool checkLabels = false)
        {
            EvaluatorState state = null;
            if (source is LiteralExpression literal)
            {
                state = EvaluateLiteralExpression(literal);
            }
            else if (source is IdentifierExpression identifier)
            {
                if (registers.TryGetValue(identifier.Identifier, out var reg))
                {
                    state = reg;
                }
                else if(enviroment.Variables.TryGetValue(identifier.Identifier, out var variable))
                {
                    state = variable;
                }
                else if (checkLabels)
                {
                    if (enviroment.Labels.TryGetValue(identifier.Identifier, out var lbl))
                    {
                        state = new EvaluatorState() { Value = lbl.ToString() };
                    }
                }
            }
            else if (source is IndexingExpression indexing)
            {
                //
                // Evaluate.
                //
                context.IsAssemblyContext = true;
                state = VisitAssemblyInstruction(indexing.Expression);
                context.IsAssemblyContext = false;
                //
                // Get the value from the heap.
                //
                var value = heap.Read((int)long.Parse(state.Value));
                state.Value = value.ToString();
            }

            return state;
        }

        private void EvaluateAsmMov(BinaryASMInstruction mov)
        {
            var sourceState = EvaluateSource(mov.Source);

            if (mov.Desination is IdentifierExpression identifierDest)
            {
                registers[identifierDest.Identifier] = sourceState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = identifierDest.Identifier, Color = Colors.White });
                    PrintAsBitSet((int)long.Parse(sourceState.Value));
                }
            }
            else if (mov.Desination is IndexingExpression indexingDest)
            {
                //
                // Evaluate.
                //
                context.IsAssemblyContext = true;
                var destState = VisitAssemblyInstruction(indexingDest.Expression);
                context.IsAssemblyContext = false;
                //
                // Get the value from the heap.
                //
                var offset = (int)long.Parse(destState.Value);
                var value = (int)long.Parse(sourceState.Value);

                var sourceValue = (int)long.Parse(sourceState.Value);
                var destValue = heap.Read(offset);

                var result = sourceValue;
                heap.Write(result, offset);

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = $"[{offset}] : {value}", Color = Colors.White });
                    PrintAsBitSet(value);
                }
            }
        }

        private void ClearFlags()
        {
            foreach (var flag in flags)
                flags[flag.Key] = 0;
        }

        private void EvaluateAsmJGE(UnaryASMInstruction jmp)
        {
            var jumpAddress = EvaluateSource(jmp.Source, checkLabels: true);

            if (flags["SF"] == flags["OF"])
            {
                ClearFlags();

                var idx = int.Parse(jumpAddress.Value);
                enviroment.StatementIndex = idx;
            }
        }

        private void EvaluateAsmJLE(UnaryASMInstruction jmp)
        {
            var jumpAddress = EvaluateSource(jmp.Source, checkLabels: true);

            if (flags["SF"] != flags["OF"] || flags["ZF"] == 1)
            {
                ClearFlags();

                var idx = int.Parse(jumpAddress.Value);
                enviroment.StatementIndex = idx;
            }
        }

        private void EvaluateAsmCmp(BinaryASMInstruction cmp)
        {
            long? source = null;
            long? destination = null;

            var sourceState = EvaluateSource(cmp.Source);

            if (cmp.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];

                source = long.Parse(sourceState.Value);
                destination = long.Parse(destState.Value);
            }
            else if (cmp.Desination is IndexingExpression indexingDest)
            {
                //
                // Evaluate.
                //
                context.IsAssemblyContext = true;
                var destState = VisitAssemblyInstruction(indexingDest.Expression);
                context.IsAssemblyContext = false;
                //
                // Get the value from the heap.
                //
                var offset = (int)long.Parse(destState.Value);
                source = long.Parse(sourceState.Value);
                destination = heap.Read(offset);
            }

            var result = destination - source;

            flags["SF"] = 0;
            flags["ZF"] = 0;
            flags["CF"] = 0;


            if (result <= -1)
                flags["SF"] = 1;

            if (result == 0)
                flags["ZF"] = 1;

            if (source > destination)
                flags["CF"] = 1;

        }

        private void EvaluateAsmAdd(BinaryASMInstruction add)
        {
            var sourceState = EvaluateSource(add.Source);

            if (add.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (long.Parse(sourceState.Value) + long.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = identifierDest.Identifier, Color = Colors.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
            else if (add.Desination is IndexingExpression indexingDest)
            {
                //
                // Evaluate.
                //
                context.IsAssemblyContext = true;
                var destState = VisitAssemblyInstruction(indexingDest.Expression);
                context.IsAssemblyContext = false;
                //
                // Get the value from the heap.
                //
                var offset = (int)long.Parse(destState.Value);

                var sourceValue = (int)long.Parse(sourceState.Value);
                var destValue = heap.Read(offset);

                var result = (sourceValue + destValue);
                heap.Write(result, offset);

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = $"[{offset}] : {result}", Color = Colors.White });
                    PrintAsBitSet(result);
                }
            }
        }

        private void EvaluateAsmSub(BinaryASMInstruction sub)
        {
            EvaluatorState sourceState = EvaluateSource(sub.Source);

            if (sub.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (long.Parse(sourceState.Value) - long.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = identifierDest.Identifier, Color = Colors.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
            else if (sub.Desination is IndexingExpression indexingDest)
            {
                //
                // Evaluate.
                //
                context.IsAssemblyContext = true;
                var destState = VisitAssemblyInstruction(indexingDest.Expression);
                context.IsAssemblyContext = false;
                //
                // Get the value from the heap.
                //
                var offset = (int)long.Parse(destState.Value);

                var sourceValue = (int)long.Parse(sourceState.Value);
                var destValue = heap.Read(offset);

                var result = (sourceValue - destValue);
                heap.Write(result, offset);

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = $"[{offset}] : {result}", Color = Colors.White });
                    PrintAsBitSet(result);
                }
            }
        }

        private void EvaluateAsmShr(BinaryASMInstruction shr)
        {
            var sourceState = EvaluateSource(shr.Source);
            
            if (shr.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (int.Parse(sourceState.Value) >> int.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = identifierDest.Identifier, Color = Colors.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
            else if (shr.Desination is IndexingExpression indexingDest)
            {
                //
                // Evaluate.
                //
                context.IsAssemblyContext = true;
                var destState = VisitAssemblyInstruction(indexingDest.Expression);
                context.IsAssemblyContext = false;
                //
                // Get the value from the heap.
                //
                var offset = (int)long.Parse(destState.Value);

                var sourceValue = (int)long.Parse(sourceState.Value);
                var destValue = heap.Read(offset);

                var result = (sourceValue >> destValue);
                heap.Write(result, offset);

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = $"[{offset}] : {result}", Color = Colors.White });
                    PrintAsBitSet(result);
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
                printer.Print(new Run() { Text = "eax", Color = Colors.White });
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
                printer.Print(new Run() { Text = "eax", Color = Colors.White });
                PrintAsBitSet((int)long.Parse(destState.Value));
            }

        }

        private void EvaluateAsmShl(BinaryASMInstruction shl)
        {
            var sourceState = EvaluateSource(shl.Source);
            
            if (shl.Desination is IdentifierExpression identifierDest)
            {
                var destState = registers[identifierDest.Identifier];
                var result = (int.Parse(sourceState.Value) << int.Parse(destState.Value));

                destState.Value = result.ToString();
                registers[identifierDest.Identifier] = destState;

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = identifierDest.Identifier, Color = Colors.White });
                    PrintAsBitSet((int)long.Parse(destState.Value));
                }
            }
            else if (shl.Desination is IndexingExpression indexingDest)
            {
                //
                // Evaluate.
                //
                context.IsAssemblyContext = true;
                var destState = VisitAssemblyInstruction(indexingDest.Expression);
                context.IsAssemblyContext = false;
                //
                // Get the value from the heap.
                //
                var offset = (int)long.Parse(destState.Value);

                var sourceValue = (int)long.Parse(sourceState.Value);
                var destValue = heap.Read(offset);

                var result = (sourceValue << destValue);
                heap.Write(result, offset);

                if (context.IsExplain)
                {
                    printer.Print(new Run() { Text = $"[{offset}] : {result}", Color = Colors.White });
                    PrintAsBitSet(result);
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
                printer.Print(new Run() { Text = operatorExpression.Operator, Color = Colors.Red });
                PrintAsBitSet((int)long.Parse(rhs.Value));
                printer.Print(new Run() { Text = "````````", Color = Colors.White });
            }

            //
            // For text operations if we have a (Any | Number) pair,
            // We're going to convert it to number.
            //
            if (lhs.Type != LiteralType.Number || rhs.Type != LiteralType.Number)
            {
                if (lhs.Type != LiteralType.Number)
                    lhs = ConvertToNumber(lhs);

                if (rhs.Type != LiteralType.Number)
                    rhs = ConvertToNumber(rhs);
            }

            var result = EvalBinaryAsNumber(@operator, lhs, rhs);

            return new EvaluatorState() { IsSigned = lhs.IsSigned, Type = lhs.Type, Value = result.ToString() };
        }

        private EvaluatorState ConvertToNumber(EvaluatorState state)
        {
            if (state.Type == LiteralType.Text)
            {
                return new EvaluatorState() { 
                    Type = LiteralType.Number,
                    Value = ((int)state.Value[0]).ToString(), 
                    IsSigned = true 
                };
            }
            else if(state.Type == LiteralType.Bool)
            {
                return new EvaluatorState()
                {
                    Type = LiteralType.Number,
                    Value = state.Value == "true" ? "1" : "0",
                    IsSigned = false
                };
            }

            return new EvaluatorState();
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

        private void PrintAsBlock()
        {
            printer.Print(Run.Yellow($"$Block"));
        }

        private void PrintAsText(string value)
        {
            printer.Print(new Run() { Text = $"\"{value}\"", Color = XConsole.ForegroundColor });
        }

        private void PrintAsBitSet(int value, int maxOffset = 6)
        {
            var val = value.ToString();
            int len = val.Length;

            List<Run> runs = new List<Run>();

            string valueToPrint = value.ToString() + new string(' ', Math.Abs(maxOffset - len)) + " => ";
            runs.Add(new Run() { Text = valueToPrint, Color = XConsole.ForegroundColor });

            for (int b = 31; b >= 0; b--)
            {
                var isSet = (value & (1 << (b % 32))) != 0;
                if (isSet)
                {
                    runs.Add(new Run() { Text = "1", Color = Colors.Green });
                }
                else
                {
                    runs.Add(new Run() { Text = "0", Color = Colors.Blue });
                }

                if (b > 0 && b % 8 == 0)
                {
                    runs.Add(new Run() { Text = "_", Color = Colors.White });
                }
            }

            printer.Print(runs.ToArray());
        }
    }
}
