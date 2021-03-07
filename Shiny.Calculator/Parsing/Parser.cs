using Shiny.Calculator.Parsing;
using Shiny.Repl.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shiny.Repl.Parsing
{
    public class Parser
    {
        private List<Token> tokens;
        private string[] commands;
        private string[] instructions;

        public Parser(string[] commands, string[] instructions)
        {
            this.commands = commands;
            this.instructions = instructions;
        }

        public AST_Node Parse(List<Token> tokens)
        {
            try
            {
                this.tokens = tokens;
                int i = 0;
                return ParseStatement(ref i);
            }
            catch(ParsingException ex)
            {
                return new AST_Error() { Line = ex.Line, Possition = ex.Position, Message = ex.Message };
            }
        }

        public AST_Node ParseStatement(ref int i)
        {
            if (IsCommand(i))
            {
                return ParseCommand(ref i);
            }
            else if (IsInstruction(i))
            {
                return ParseInstruction(ref i);
            }
            else
            {
                return ParseBinaryExpression(ref i, 0);
            }
        }

        private ASM_Instruction ParseInstruction(ref int i)
        {
            var name = tokens[i].GetValue().ToLower();

            if (name == "mov")
            {
                return ParseTwoArgInstruction(name, ref i);
            }
            else if (name == "add")
            {
                return ParseTwoArgInstruction(name, ref i);
            }
            else if (name == "sub")
            {
                return ParseTwoArgInstruction(name, ref i);
            }
            else if (name == "shl")
            {
                return ParseTwoArgInstruction(name, ref i);
            }
            else if (name == "shr")
            {
                return ParseTwoArgInstruction(name, ref i);
            }
            else if(name == "mul")
            {
                return ParseSingleArgInstruction(name, ref i);
            }
            else if (name == "div")
            {
                return ParseSingleArgInstruction(name, ref i);
            }

            throw ParsingError("Assembly Instruction Error - Unknown Instruction", i);
        }

        private ASM_Instruction ParseSingleArgInstruction(string name, ref int i)
        {
            if (IsLiteralsOrVarsOrIndexing(i + 1) == false)
                throw ParsingError("Assembly Instruction Error - Missing arguments", i);

            Move(ref i);
            //
            // INST A
            //
            var source = ParseInstructionArgument(ref i);

            return new UnaryASMInstruction()
            {
                Name = name,
                Source = source
            };
        }

        private AST_Node ParseInstructionArgument(ref int i)
        {
            if (IsIndexingAccessOpen(i))
            {
                IndexingExpression indexing = new IndexingExpression()
                {
                    Expression = ParseBinaryExpression(ref i, 0, true)
                };
                return indexing;
            }
            else if (IsLiteralsOrVars(i))
            {
                return ParseLiteralsAndIdentifiers(ref i);
            }

            throw ParsingError("Assembly Instruction Error - Invalid Argument", i);
        }

        private ASM_Instruction ParseTwoArgInstruction(string name, ref int i)
        {
            if(IsLiteralsOrVarsOrIndexing(i + 1) == false)
                throw ParsingError("Assembly Instruction Error - Missing argument", i);

            Move(ref i);

            //
            // INST A,B
            //
            AST_Node dest = ParseInstructionArgument(ref i);

            if (IsComma(i + 1) == false && IsLiteralsOrVarsOrIndexing(i + 2))
                throw ParsingError("Assembly Instruction Error - Missing second argument", i);

            if (IsComma(i + 1) == false)
                throw ParsingError("Assembly Instruction Error - Invalid Argument (missing comma)", i);

            Move(ref i);
            Move(ref i);
            
            AST_Node source = ParseInstructionArgument(ref i);

            return new BinaryASMInstruction()
            {
                Name = name,
                Desination = dest,
                Source = source
            };
        }

        private CommandExpression ParseCommand(ref int i)
        {
            var token = tokens[i];
            Move(ref i);
            var rhs = ParseBinaryExpression(ref i, 0);

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
        private AST_Node ParseBinaryExpression(ref int i, int level, bool isAssemblyAdressing = false)
        {
            AST_Node left = null;

            for (; i < tokens.Count; i++)
            {
                 var token = tokens[i];

                //
                // Check if we're dealing with Unary Expression: (-1 -2 etc)
                // The Left Hand side needs to be unset for this to be true
                // since otherwise we would match against any substraction.
                // But since we descent on every oprtator unary expressions will be 
                // correctly matched.
                //
                if (IsLiteralWithOperator(i) && left == null)
                {
                    UnaryExpression unary = new UnaryExpression();
                    unary.Operator = token.GetValue();
                    Move(ref i);

                    unary.Left = ParseLiteralsAndIdentifiers(ref i);
                    left = unary;
                }
                else if(IsParrenWithOperator(i) && left == null)
                {
                    UnaryExpression unary = new UnaryExpression();
                    unary.Operator = token.GetValue();

                    Move(ref i);
                    Move(ref i);

                    unary.Left = ParseBinaryExpression(ref i, 0);
                    left = unary;
                }
                else if (IsOperator(i))
                {
                    var @operator = (OperatorToken)token;

                    //
                    // Check if we should descent deeper, if the operator has a
                    // higher level value we simply call parse binary again.
                    //
                    if (@operator.Level > level)
                    {
                        Move(ref i);
                        var right = ParseBinaryExpression(ref i, @operator.Level, isAssemblyAdressing);
                        left = new BinaryExpression() { Left = left, Operator = @operator.GetValue(), Right = right };
                    }
                    else
                    {
                        //
                        // Move back to the operator
                        // This could be improved by explicit itteration control 
                        // but it's not needed for now.
                        //
                        i--;
                        return left;
                    }
                }

                //
                // @IDEA:
                // Indexing Access has sense in assembly simulation
                // Perhaps we should pass it as an argument to recursive descent.
                //
                else if(IsIndexingAccessOpen(i))
                {
                    Move(ref i);
                    return ParseBinaryExpression(ref i, 0, isAssemblyAdressing);
                }
                else if(IsIndexingAccessClose(i))
                {
                    return left;
                }
                //
                // For Assembly index addressing we need to 
                // exit when we encouter a comma.
                //
                else if(IsComma(i) && isAssemblyAdressing)
                {
                    i--;
                    return left;
                }
                else if(IsOpenBracket(i))
                {
                    Move(ref i);
                    return ParseBinaryExpression(ref i, 0);
                }
                else if(IsCloseBracket(i))
                {
                    return left;
                }
                else if (IsLiteralsOrVars(i))
                {
                    left = ParseLiteralsAndIdentifiers(ref i);
                }
            }

            return left;
        }

        private AST_Node ParseLiteralsAndIdentifiers(ref int i)
        {
            var token = tokens[i];

            if (IsNumber(i))
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
            else if (IsText(i))
            {
                LiteralExpression text = new LiteralExpression() { Value = token.GetValue(), Type = LiteralType.Text };
                return text;
            }
            else if (IsBool(i))
            {
                LiteralExpression boolean = new LiteralExpression() { Value = token.GetValue(), Type = LiteralType.Bool };
                return boolean;
            }
            else if (IsWord(i))
            {
                var variable = new IdentifierExpression() { Identifier = token.GetValue() };
                return variable;
            }

            throw ParsingError("Binary Expression Error (Left) - Incorect Token", i);
        }

        private int Move(ref int i, Predicate<int> assert = null)
        {
            i++;
            if (assert != null)
            {
                if (assert(i) == false)
                {
                    throw new ArgumentException($"Expected: {assert.Method}");
                }
            }

            return i;
        }

        private bool IsOperator(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "Operator" || token.TokenName == "BinaryOperator")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsParrenWithOperator(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if ((token.TokenName == "Operator" || token.TokenName == "BinaryOperator") &&
                    (token.GetValue() == "-" || token.GetValue() == "~"))
                {
                    return IsOpenBracket(tokenIndex + 1);
                }
            }

            return false;
        }

        private bool IsOpenBracket(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "BracketOpen" && token.GetValue() == "(")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCloseBracket(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "BracketClose" && token.GetValue() == ")")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsLiteralWithOperator(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if ((token.TokenName == "Operator" || token.TokenName == "BinaryOperator") &&
                    (token.GetValue() == "-" || token.GetValue() == "~"))
                {
                    return IsNumber(tokenIndex + 1) || IsWord(tokenIndex + 1);
                }
            }

            return false;
        }

        private bool IsBool(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "Word" && (token.GetValue() == "true" || token.GetValue() == "false"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsText(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "Text")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsLiteralsOrVars(int tokenIndex)
        {
            return IsNumber(tokenIndex) || IsText(tokenIndex) || IsWord(tokenIndex) || IsBool(tokenIndex);
        }

        private bool IsLiteralsOrVarsOrIndexing(int tokenIndex)
        {
            return IsNumber(tokenIndex) || IsText(tokenIndex) || IsWord(tokenIndex) || IsBool(tokenIndex) || IsIndexingAccessOpen(tokenIndex);
        }

        private bool IsCommand(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];
                var value = token.GetValue();

                if (token.TokenName == "Word" && commands.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWord(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "Word")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsComma(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "Separator" && token.GetValue() == ",")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNumber(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "Number")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsIndexingAccessOpen(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "BracketOpen")
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsIndexingAccessClose(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];

                if (token.TokenName == "BracketClose")
                {
                    return true;
                }
            }

            return false;
        }


        private bool IsInstruction(int tokenIndex)
        {
            if (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];
                var value = token.GetValue();

                if (token.TokenName == "Word" && instructions.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        private ParsingException ParsingError(string message, int i)
        {
            int line = -1;
            int pos  = -1; 

            if(i < tokens.Count)
            {
                line = tokens[i].Line;
                pos  = tokens[i].Position;
            }

            return new ParsingException(message, line, pos);
        }
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
        public string Message { get; set; }
    }

    public class AST_Node
    {
        private static int IdGen = 0;
        public int Line { get; set; }
        public int Possition { get; set; }
        public string Name { get; set; }

        public virtual void Print()
        {
        }

        public AST_Node()
        {
            Name = IdGen++.ToString();
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
        Function
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
