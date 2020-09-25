﻿using Shiny.Repl.Tokenization;
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

        public Parser(string[] commands)
        {
            this.commands = commands;
        }

        public Expression Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            int i = 0;
            return ParseStatement(ref i);
        }

        public Expression ParseStatement(ref int i)
        {
            if(IsCommand(i))
            {
                return ParseCommand(ref i);
            }
            else
            {
                return ParseBinaryExpression(ref i, 0);
            }
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
        private Expression ParseBinaryExpression(ref int i, int level)
        {
            Expression left = null;

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
                        var right = ParseBinaryExpression(ref i, @operator.Level);
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
                else if(IsOpenBracket(i))
                {
                    Move(ref i);
                    return ParseBinaryExpression(ref i, 0);
                }
                else if(IsCloseBracket(i))
                {
                    return left;
                }
                else if (IsLiteralsAndVars(i))
                {
                    left = ParseLiteralsAndIdentifiers(ref i);
                }
            }

            return left;
        }

        private Expression ParseLiteralsAndIdentifiers(ref int i)
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

            throw new ArgumentException("Binary Expression Error (Left) - Incorect Token");
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

        private bool IsLiteralsAndVars(int tokenIndex)
        {
            return IsNumber(tokenIndex) || IsText(tokenIndex) || IsWord(tokenIndex) || IsBool(tokenIndex);
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
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; set; }
        public Expression Right { get; set; }
        public string Operator { get; set; }

        public override void Print()
        {
            Console.WriteLine($"{Left.ToString()} {Operator} {Right.ToString()}");
        }
    }

    public class UnaryExpression : Expression
    {
        public Expression Left { get; set; }
        public string Operator { get; set; }
    }

    public class Expression
    {
        private static int IdGen = 0;
        public int Line { get; set; }
        public int Possition { get; set; }
        public string Name { get; set; }

        public virtual void Print()
        {
        }

        public Expression()
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

    public class LiteralExpression : Expression
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
                return $"'{Value}'";
            }
        }
        public override void Print()
        {
            Console.WriteLine($"LIT-{Name} = {Value}");
        }
    }

    public class CommandExpression : Expression
    {
        public string CommandName { get; set; }
        public Expression RightHandSide { get; set; }
    }

    public class IdentifierExpression : Expression
    {
        public LiteralType Type { get; set; }
        public string TypeName { get; set; }
        public string Identifier { get; set; }

        public override string ToString()
        {
            return $"{Identifier}";
        }
    }
}
