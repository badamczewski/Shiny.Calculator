using Shiny.Calculator.Parsing;
using Shiny.Calculator.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shiny.Calculator.Parsing
{
    public class Parser
    {
        private List<AST_Error> errors = null;
        private List<Token> tokens;
        private bool isBlock = false;
        private string[] commands;
        private string[] instructions;
        private string[] operators;

        public Parser(string[] commands, string[] operators, string[] instructions)
        {
            this.commands = commands;
            this.instructions = instructions;
            this.operators = operators;
        }

        public AST Parse(List<Token> tokens)
        {
            errors = new List<AST_Error>();
            AST syntaxTree = new AST();
            syntaxTree.Statements = new List<AST_Node>();

            try
            {
                this.tokens = tokens;
                int index = 0;

                for (; index < tokens.Count; index++)
                {
                    if (IsEndOfStatement(index)) continue;

                    var stmt = ParseStatement(ref index);
                    syntaxTree.Statements.Add(stmt);
                }

                syntaxTree.Errors = errors;
                return syntaxTree;
            }
            catch(Exception ex)
            {
                ParsingError(ex.Message, -1);
                return syntaxTree;
            }
        }

        public AST_Node ParseStatement(ref int index)
        {
            if (IsCommand(index))
            {
                return ParseCommand(ref index);
            }
            else if (IsInstruction(index))
            {
                return ParseInstruction(ref index);
            }
            else if (IsVariableAssigmnent(index))
            {
                return ParseVariableAssigment(ref index);
            }
            else if (IsLiteralsOrVars(index) || IsOpenBracket(index) || IsParrenWithOperator(index))
            {
                return ParseBinaryExpression(ref index, 0);
            }
            else if(IsOpenBlock(index))
            {
                return ParseBlock(ref index);
            }

            return ParsingError("Unknown statement", index);
        }

        private AST_Node ParseVariableAssigment(ref int index)
        {
            //
            // Create the identifier.
            // 
            var variable = new IdentifierExpression() 
            { 
                Identifier = tokens[index].GetValue() 
            };
            //
            // Shoud be equals operator.
            // This doesn't need to be checked since we already did when we entered this method.
            Move(ref index);
            //
            // Move to the acutal assigment.
            //
            var wasLast = Move(ref index);
            //
            // Check for empty assigment.
            //
            if (wasLast)
                ParsingError("Invalid or empty assigment.", "Block, expression or value recquired.", index - 1);
            //
            // Check if this is a block assigment or just a binary expression. 
            //
            if (IsOpenBlock(index))
            {
                //
                // Parse blocks as programs that have a list of statements.
                //
                var block = ParseBlock(ref index);

                return new VariableAssigmentExpression()
                {
                    Identifier = variable,
                    Assigment = block
                };
            }
            else
            {
                var assigment = ParseBinaryExpression(ref index, 0);
                return new VariableAssigmentExpression()
                {
                    Identifier = variable,
                    Assigment = assigment
                };
            }
        }

        private AST_Node ParseBlock(ref int index)
        {
            Move(ref index);
            isBlock = true;

            List<AST_Node> nodes = new List<AST_Node>();

            for (; index < tokens.Count; index++)
            {
                //
                // Remove empty lines and break on block close '}'
                //
                if (IsEndOfStatement(index)) continue;
                if (IsCloseBlock(index)) { break; }

                var stmt = ParseStatement(ref index);
                nodes.Add(stmt);

            }

            isBlock = false;

            return new BlockExpression() { Body = nodes };
        }

        private AST_Node ParseInstruction(ref int index)
        {
            var name = tokens[index].GetValue().ToLower();

            if (name == "mov")
            {
                return ParseTwoArgInstruction(name, ref index);
            }
            else if (name == "add")
            {
                return ParseTwoArgInstruction(name, ref index);
            }
            else if (name == "sub")
            {
                return ParseTwoArgInstruction(name, ref index);
            }
            else if (name == "shl")
            {
                return ParseTwoArgInstruction(name, ref index);
            }
            else if (name == "shr")
            {
                return ParseTwoArgInstruction(name, ref index);
            }
            else if (name == "cmp")
            {
                return ParseTwoArgInstruction(name, ref index);
            }
            else if(name == "jle")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if (name == "jge")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if (name == "jl")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if (name == "jg")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if (name == "je")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if (name == "jne")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if(name == "mul")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if (name == "div")
            {
                return ParseSingleArgInstruction(name, ref index);
            }
            else if(name == "label")
            {
                Move(ref index);
                var labelName = tokens[index].GetValue();
                Move(ref index);
                return new AST_Label() { Label = labelName };
            }
 
            return ParsingError("Unknown assembly instruction.", index);
        }

        private AST_Node ParseSingleArgInstruction(string name, ref int index)
        {
            if (IsLiteralsOrVarsOrIndexing(index + 1) == false)
                return ParsingError("Missing instruction arguments.", index);

            Move(ref index);
            //
            // INST A
            //
            var source = ParseInstructionArgument(ref index);

            return new UnaryASMInstruction()
            {
                Name = name,
                Source = source
            };
        }

        private AST_Node ParseInstructionArgument(ref int index)
        {
            if (IsIndexingAccessOpen(index))
            {
                IndexingExpression indexing = new IndexingExpression()
                {
                    Expression = ParseBinaryExpression(ref index, 0, true)
                };
                return indexing;
            }
            else if (IsLiteralsOrVars(index))
            {
                return ParseLiteralsAndIdentifiers(ref index);
            }

            return ParsingError("Invalid instruction argument", index);
        }

        private AST_Node ParseTwoArgInstruction(string name, ref int index)
        {
            if(IsLiteralsOrVarsOrIndexing(index + 1) == false)
                return ParsingError("Missing instruction arguments.", index);

            Move(ref index);

            //
            // INST A,B
            //
            AST_Node dest = ParseInstructionArgument(ref index);

            if (IsComma(index + 1) == false && IsLiteralsOrVarsOrIndexing(index + 2))
                return ParsingError("Missing comma between arguments.", index);

            if (IsComma(index + 1) == false)
                return ParsingError("This instruction requires two arguments.", index);

            Move(ref index);
            Move(ref index);
            
            AST_Node source = ParseInstructionArgument(ref index);

            return new BinaryASMInstruction()
            {
                Name = name,
                Desination = dest,
                Source = source
            };
        }

        private CommandExpression ParseCommand(ref int index)
        {
            var token = tokens[index];
            Move(ref index);
            var rhs = ParseBinaryExpression(ref index, 0);

            return new CommandExpression() { CommandName = token.GetValue(), RightHandSide = rhs };
        }

        #region How_Binary_Expression_Parser_Works
        //
        // The binary expression parser uses a form of a Recursive Decent Parser to handle Operator-precedence
        // our function to parse expression has a level variable that determines if we should
        // parse recursivley and build a nested tree down or if we should exit 
        // (bc we have an operator with a higher level then we do)
        // 
        // This is a very simple and roboust solution as compared to the Shift Reducre Techniques with minimal
        // to almost no performance drawbacks.
        //
        // See for yourself:
        // (this example is not a 1-to-1 implementation but it's the core idea how we do things)
        //
        // 2 * 3 * 4 * 5 + 6 * 7 + 8
        // parse_expression(-100)
        //   2
        //   * (100)
        //   100 > -100 TRUE
        //   parse_expression(100)
        //     3
        //     * (100)
        //     100 > 100 TRUE
        //     parse_expression(100)
        //       4
        //       * (100)
        //       100 > 100 TRUE
        //       parse_expression(100)
        //         5
        //         + (50)
        //         50 > 100 FALSE
        //         return 5;
        //       exp = (l= 4 * r = 5)
        //     exp = (l = 3 * r = exp(4 * 5))   
        //   exp = (l = 2 * exp(3 * 4 * 5))
        //   + (50) 
        //   50 > -100 TRUE  
        //   parse_expression(50)
        //     6
        //     * (100)
        //     100 > 50 TRUE
        //     parse_expression(100)
        //       7
        //       + (50)
        //       50 > 100 FALSE
        //       return 7;
        //     exp = (l = 6 * r = 7)
        //   exp = (l = prev_exp, r = exp(6 * 7)
        //   + (50)
        //   50 >= -100 TRUE
        //   parse_expression(50)
        //     8
        //     EOF
        //     return 8;
        //   exp = (l = prev_exp, r = 8)
        //
        #endregion
        private AST_Node ParseBinaryExpression(ref int index, int level, bool isAssemblyAdressing = false)
        {
            AST_Node left = null;

            for (; index < tokens.Count; index++)
            {
                 var token = tokens[index];

                //
                // Check if we're dealing with Unary Expression: (-1 -2 etc)
                // The Left Hand side needs to be unset for this to be true
                // since otherwise we would match against any substraction.
                // But since we descent on every oprtator unary expressions will be 
                // correctly matched.
                //
                if (IsLiteralWithOperator(index) && left == null)
                {
                    UnaryExpression unary = new UnaryExpression();
                    unary.Operator = token.GetValue();
                    Move(ref index);

                    unary.Left = ParseLiteralsAndIdentifiers(ref index);
                    left = unary;
                }
                else if(IsParrenWithOperator(index) && left == null)
                {
                    UnaryExpression unary = new UnaryExpression();
                    unary.Operator = token.GetValue();

                    Move(ref index);
                    Move(ref index);

                    unary.Left = ParseBinaryExpression(ref index, 0);
                    left = unary;
                }
                else if (IsOperator(index))
                {
                    var @operator = (OperatorToken)token;

                    var isValid = ValidateBinaryExpressionOperator(@operator, index);

                    // 
                    // Check if we should descent deeper, if the operator has a
                    // higher level value we simply call parse binary again.
                    //
                    if (@operator.Level > level)
                    {
                        Move(ref index);
                        var right = ParseBinaryExpression(ref index, @operator.Level, isAssemblyAdressing);
                        left = new BinaryExpression() { Left = left, Operator = @operator.GetValue(), Right = right };
                    }
                    else
                    {
                        //
                        // Move back to the operator
                        // This could be improved by explicit itteration control 
                        // but it's not needed for now.
                        //
                        if (isValid) index--;
                        return left;
                    }
                }

                //
                // @IDEA:
                // Indexing Access has sense in assembly simulation
                // Perhaps we should pass it as an argument to recursive descent.
                //
                else if(IsIndexingAccessOpen(index))
                {
                    Move(ref index);
                    return ParseBinaryExpression(ref index, 0, isAssemblyAdressing);
                }
                else if(IsIndexingAccessClose(index))
                {
                    return left;
                }
                //
                // For Assembly index addressing we need to 
                // exit when we encouter a comma.
                //
                else if(IsComma(index) && isAssemblyAdressing)
                {
                    index--;
                    return left;
                }
                else if(IsOpenBracket(index))
                {
                    Move(ref index);
                    return ParseBinaryExpression(ref index, 0);
                }
                else if(IsCloseBracket(index))
                {
                    return left;
                }
                else if (IsLiteralsOrVars(index))
                {
                    left = ParseLiteralsAndIdentifiers(ref index);
                }
                //
                // IF we are parsing a block we need to check for block temrination
                //
                else if(isBlock && IsCloseBlock(index))
                {
                    return left;
                }
                else if (isBlock && IsEndOfStatement(index))
                {
                    return left;
                }
                else
                {
                    return ParsingError("Unexpected sub-expression.", index);
                }
            }

            return left;
        }
  
        private bool ValidateBinaryExpressionOperator(OperatorToken @operator, int index)
        {
            var isValid = true;
            //
            // Check if this operator is even valid.
            // IDEA: We could do it on the tokenization level, but for now
            // let's push this to the parser.
            //
            if (operators.Contains(@operator.GetValue()) == false)
            {
                ParsingError("Invalid operator.", $"Supported Operators are: ({string.Join(", ", operators)})", index);
                isValid = false;
            }

            //
            // Cannot end on operator.
            //
            if (IsLastToken(index))
            {
                ParsingError("Operator cannot end the expression.", index);
                isValid = false;
            }

            //
            // Validate if the next token is a valid one.
            //
            if (IsOperator(index + 1) && (IsLiteralWithOperator(index + 1) == false && IsParrenWithOperator(index + 1) == false))
            {
                ParsingError("Expected literal or variable, but got operator.", index + 1);
                isValid = false;
            }

            return isValid;
        }

        private AST_Node ParseLiteralsAndIdentifiers(ref int index)
        {
            var token = tokens[index];

            if (IsNumber(index))
            {
                object value = null;
                var numToken = (NumberToken)token;
                var numValue = numToken.GetValue();
                numValue = numValue.Replace("_", string.Empty);

                if (numToken.Format == 'x')
                {
                    if (numToken.IsSigned)
                        value = Convert.ToInt64(numValue, 16);
                    else
                        value = Convert.ToUInt64(numValue, 16);
                }
                else if (numToken.Format == 'b')
                {
                    value = Convert.ToInt64(numValue, 2);
                }
                else
                {
                    value = Convert.ToInt64(numValue, 10);
                }

                LiteralExpression number = new LiteralExpression()
                {
                    IsSigned = numToken.IsSigned,
                    Value = value.ToString(),
                    Raw = numToken.GetValue(),
                    Type = LiteralType.Number
                };

                return number;
            }
            else if (IsText(index))
            {
                LiteralExpression text = new LiteralExpression() { Value = token.GetValue(), Type = LiteralType.Text };
                return text;
            }
            else if (IsBool(index))
            {
                LiteralExpression boolean = new LiteralExpression() { Value = token.GetValue(), Type = LiteralType.Bool };
                return boolean;
            }
            else if (IsWord(index))
            {
                var variable = new IdentifierExpression() { Identifier = token.GetValue() };
                return variable;
            }

            return ParsingError("Incorrect sub-expression.", index);
        }

        private bool Move(ref int index)
        {
            index++;
            return (index < tokens.Count) == false;
        }

        private bool IsOperator(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.Operator, TokenKind.BinaryOperator);
        }

        private bool IsParrenWithOperator(int tokenIndex)
        {
            if(Match(tokenIndex, (TokenKind.Operator, "-"), (TokenKind.BinaryOperator, "~")))
                return IsOpenBracket(tokenIndex + 1);

            return false;
        }

        private bool IsEndOfStatement(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.EOS);
        }

        private bool IsLastToken(int tokenIndex)
        {
            return tokenIndex >= tokens.Count - 1;
        }

        private bool IsOpenBlock(int tokenIndex)
        {
            return Match(tokenIndex, (TokenKind.BlockOpen, "{"));
        }

        private bool IsCloseBlock(int tokenIndex)
        {
            return Match(tokenIndex, (TokenKind.BlockClose, "}"));
        }

        private bool IsOpenBracket(int tokenIndex)
        {
            return Match(tokenIndex, (TokenKind.BracketOpen, "("));
        }

        private bool IsCloseBracket(int tokenIndex)
        {
            return Match(tokenIndex, (TokenKind.BracketClose, ")"));
        }

        private bool IsLiteralWithOperator(int tokenIndex)
        {
            if (Match(tokenIndex, (TokenKind.Operator, "-"), (TokenKind.BinaryOperator, "~")))
            {
                return IsNumber(tokenIndex + 1) || IsWord(tokenIndex + 1);
            }
            return false;
        }

        private bool IsBool(int tokenIndex)
        {
            return Match(tokenIndex, (TokenKind.Word, "true"), (TokenKind.Word, "false"));
        }

        private bool IsText(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.Text);
        }

        private bool IsLiteralsOrVars(int tokenIndex)
        {
            return  IsNumber(tokenIndex) ||
                    IsText(tokenIndex)   ||
                    IsWord(tokenIndex)   ||
                    IsBool(tokenIndex)   ||
                    IsLiteralWithOperator(tokenIndex);
        }

        private bool IsLiteralsOrVarsOrIndexing(int tokenIndex)
        {
            return IsNumber(tokenIndex) || 
                   IsText(tokenIndex)   || 
                   IsWord(tokenIndex)   || 
                   IsBool(tokenIndex)   || 
                   IsIndexingAccessOpen(tokenIndex);
        }

        private bool IsCommand(int tokenIndex)
        {
            if (Match(tokenIndex, TokenKind.Word))
            {
                var token = tokens[tokenIndex];
                var value = token.GetValue();

                if (commands.Contains(value))
                    return true;
            }

            return false;
        }

        private bool IsWord(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.Word);
        }

        private bool IsComma(int tokenIndex)
        {
            return Match(tokenIndex, (TokenKind.Separator, ","));
        }

        private bool IsNumber(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.Number);
        }

        private bool IsIndexingAccessOpen(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.BracketOpen);
        }

        private bool IsIndexingAccessClose(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.BracketClose);
        }

        private bool IsVariableAssigmnent(int tokenIndex)
        {
            return Match(tokenIndex, TokenKind.Word) &&
                   Match(tokenIndex + 1, (TokenKind.Operator, "="));
        }

        private bool IsInstruction(int tokenIndex)
        {
            if (Match(tokenIndex, TokenKind.Word))
            {
                var token = tokens[tokenIndex];
                var value = token.GetValue();

                if (instructions.Contains(value))
                    return true;
            }

            return false;
        }

        private bool Match(int tokenIndex, params TokenKind[] kinds)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                foreach (var kind in kinds)
                {
                    if (token.Kind == kind)
                        return true;
                }

                return false;
            }
            return false;
        }

        private bool Match(int tokenIndex, params (TokenKind kind, string value)[] matches)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];
                bool kindMatch = false, valueMatch = false;

                foreach (var match in matches)
                {
                    if (token.Kind == match.kind)
                        kindMatch = true;
                    if (token.GetValue() == match.value)
                        valueMatch = true;

                    if (kindMatch && valueMatch) return true;
                }

                return false;
            }
            return false;
        }

        private AST_Node ParsingError(string errorMessage, int i)
        {
            return ParsingError(errorMessage, null, i);
        }

        private AST_Node ParsingError(string errorMessage, string helpMessage, int i)
        {
            int line = -1;
            int pos  = -1;

            Token[] tokensOnTheSameLine = null;

            if (i < tokens.Count)
            {
                line = tokens[i].Line;
                pos  = tokens[i].Position;

                tokensOnTheSameLine = tokens.Where(x => x.Line == line).ToArray();
            }

            var error = new AST_Error()
            {
                ErrorMessage = errorMessage,
                HelpMessage = helpMessage,
                Line = line, Possition = pos, 
                SurroundingTokens = tokensOnTheSameLine 
            };

            errors.Add(error);
            return error;
        }
    }

    public class AST_Label : AST_Node
    {
        public string Label { get; set; }
    }

    public class BinaryExpression : AST_Node
    {
        public AST_Node Left { get; set; }
        public AST_Node Right { get; set; }
        public string Operator { get; set; }

        public override void Print()
        {
            Console.WriteLine($"BINARY-{Name} = {Operator}");
            Left.Print();
            Right.Print();
        }
    }

    public class UnaryExpression : AST_Node
    {
        public AST_Node Left { get; set; }
        public string Operator { get; set; }

        public override void Print()
        {
            Console.WriteLine($"UNARY-{Name} = {Operator}");
            Left.Print();
        }
    }

    public class AST_Error : AST_Node
    {
        public string ErrorMessage { get; set; }
        public string HelpMessage { get; set; }
        public Token[] SurroundingTokens { get; set; }
    }

    public class AST_Node
    {
        private static int IdGen = 0;
        public int Line { get; set; }
        public int Possition { get; set; }
        public string Name { get; set; }

        //
        // Relative address, since this language mixes a bunch of 
        // conflicting things like, maths, programming and x86 assembly
        // each node in the AST can be addressed using an artificial address.
        // This will allow us to later create jump and goto instructions that
        // will reference this address.
        //
        public string Address { get; set; }

        public virtual void Print()
        {
        }

        public AST_Node()
        {
            var id = IdGen++.ToString();
            Name = id;
            Address = id; 
        }

        public static void ResetID()
        {
            IdGen = 0;
        }
    }

    public enum LiteralType : byte
    {
        Any,
        Bool,
        Number,
        Text,
        Array,
        Void,
        Struct,
        Function,
        Block
    }

    public class LiteralExpression : AST_Node
    {
        public LiteralType Type { get; set; }
        public string Value { get; set; }
        public string Raw { get; set; }
        public bool IsSigned { get; set; }

        public override string ToString()
        {
            if (Type == LiteralType.Number || Type == LiteralType.Bool)
            {
                return $"{Value}";
            }
            else
            {
                return $"\"{Value}\"";
            }
        }

        public override void Print()
        {
            Console.WriteLine($"LIT-{Name} = {Value}");
        }
    }

    public class CommandExpression : AST_Node
    {
        public string CommandName { get; set; }
        public AST_Node RightHandSide { get; set; }

        public override void Print()
        {
            RightHandSide.Print();
        }
    }

    public class ASM_Instruction : AST_Node
    {
        public override void Print()
        {
            Console.WriteLine($"ASM-{Name}");
        }
    }

    public class BinaryASMInstruction : ASM_Instruction
    {
        public AST_Node Source { get; set; }
        public AST_Node Desination { get; set; }
    }

    public class UnaryASMInstruction : ASM_Instruction
    {
        public AST_Node Source { get; set; }
    }

    public class IndexingExpression : AST_Node
    {
        public AST_Node Expression { get; set; }
    }

    public class BlockExpression : AST_Node
    {
        public List<AST_Node> Body { get; set; }
        public Dictionary<string, int> LabelToAddressMap { get; set; }
    }

    public class VariableAssigmentExpression : AST_Node
    {
        public IdentifierExpression Identifier { get; set; }
        public AST_Node Assigment { get; set; }
    }

    public class IdentifierExpression : AST_Node
    {
        public LiteralType Type { get; set; }
        public string TypeName { get; set; }
        public string Identifier { get; set; }

        public override string ToString()
        {
            return $"{Identifier}";
        }

        public override void Print()
        {
            Console.WriteLine($"IDEN-{Name} = {Identifier}");
        }
    }
}
