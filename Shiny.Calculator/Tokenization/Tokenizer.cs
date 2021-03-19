using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Shiny.Repl.Tokenization
{
    public class Tokenizer
    {
        private ReadOnlyMemory<char> input;
        private char newLineChar = '\n';
        private char[] hexTable = new char[] {
            'A', 'B', 'C', 'D', 'E', 'F',
            'a', 'b', 'c', 'd', 'e', 'f'
        };

        public List<Token> Tokenize(string program, char newLineChar = '\n')
        {
            List<Token> tokens = new List<Token>();

            input = program.AsMemory();
            this.newLineChar = newLineChar;

            int line = 1;
            int offset = 0;
            int index = 0;

            while (index < input.Length)
            {
                var c = input.Span[index];

                if (c == newLineChar)
                {
                    tokens.Add(new Token(TokenKind.EOS) { Line = line, Position = offset, Input = input.Slice(index, 1) });

                    index++;
                    line++;
                    offset = 0;
                }
                else if (IsColon(index))
                {
                    var colon = ParseColon(ref index, ref line, ref offset);
                    tokens.Add(colon);
                }
                else if (char.IsDigit(c))
                {
                    var number = ParseNumber(ref index, ref line, ref offset);
                    tokens.Add(number);
                }
                else if (IsCommand(index))
                {
                    var command = ParseCommand(ref index, ref line, ref offset);
                    tokens.Add(command);
                }
                else if (IsDot(index) && IsDot(index + 1))
                {
                    var range = ParseRange(ref index, ref line, ref offset);
                    tokens.Add(range);
                }
                else if (IsComment(index) && IsComment(index + 1))
                {
                    var comment = ParseComment(ref index, ref line, ref offset);
                    tokens.Add(comment);
                }
                else if (IsDot(index))
                {
                    var dot = ParseDot(ref index, ref line, ref offset);
                    tokens.Add(dot);
                }
                else if (IsBinaryOperator(index))
                {
                    var @operator = ParseBinaryOperator(ref index, ref line, ref offset);
                    tokens.Add(@operator);
                }
                else if (IsChar(index, '=') && (IsBinaryOperator(index + 1) || IsChar(index + 1, '=')))
                {
                    var @operator = ParseBinaryOperator(ref index, ref line, ref offset);
                    tokens.Add(@operator);
                }
                else if (IsOperator(index))
                {
                    var @operator = ParseOperator(ref index, ref line, ref offset);
                    tokens.Add(@operator);
                }
                else if (IsBlockOpen(index))
                {
                    var block = ParseBlockOpen(ref index, ref line, ref offset);
                    tokens.Add(block);
                }
                else if (IsBlockClose(index))
                {
                    var block = ParseBlockClose(ref index, ref line, ref offset);
                    tokens.Add(block);
                }
                else if (IsBracketOpen(c))
                {
                    var bracket = ParseBracketOpen(ref index, ref line, ref offset);
                    tokens.Add(bracket);
                }
                else if (IsBracketClose(c))
                {
                    var bracket = ParseBracketClose(ref index, ref line, ref offset);
                    tokens.Add(bracket);
                }
                else if (IsSeparator(c))
                {
                    var separator = ParseSeparator(ref index, ref line, ref offset);
                    tokens.Add(separator);
                }
                else if (char.IsLetter(c))
                {
                    var word = ParseKeywordOrVar(ref index, ref line, ref offset);
                    tokens.Add(word);
                }
                else if (IsQuote(c))
                {
                    //
                    // Move since this symbol is the Quote.
                    //
                    index++;
                    var text = ParseText(ref index, c, ref line, ref offset);
                    tokens.Add(text);
                    index++;

                    offset += 2;
                }
                else if (IsEOF(c))
                {
                    var eof = ParseEndOfStatement(ref index, ref line, ref offset);
                    tokens.Add(eof);
                }
                else
                {
                    index++;
                    offset++;
                }
            }

            return tokens;
        }

        public bool IsDot(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == '.')
                    return true;
            }
            return false;
        }

        public bool IsBlockOpen(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == '{')
                    return true;
            }
            return false;
        }

        public bool IsBlockClose(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == '}')
                    return true;
            }
            return false;
        }

        public bool IsCommand(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == '#')
                    return true;
            }
            return false;
        }

        public bool IsColon(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == ':')
                    return true;
            }
            return false;
        }

        public bool IsComment(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == '/')
                    return true;
            }
            return false;
        }

        public bool IsChar(int i, char check)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == check)
                    return true;
            }
            return false;
        }

        public bool IsOperator(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == '+' || c == '-' || c == '*' || c == '/' || c == '=' || c == '%')
                    return true;
            }
            return false;
        }

        public bool IsBinaryOperator(int i)
        {
            if (i < input.Length)
            {
                var c = input.Span[i];
                if (c == '>' || c == '<' || c == '!' || c == '&' || c == '|' || c == '^' || c == '~')
                    return true;
            }
            return false;
        }

        public bool IsSeparator(char c)
        {
            return c == ',';
        }

        private bool IsQuote(char c)
        {
            return c == '\'' || c == '\"';
        }

        private bool IsEOF(char c)
        {
            return c == ';';
        }

        private bool IsBracketClose(char c)
        {
            return c == ')' || c == ']';
        }

        private bool IsBracketOpen(char c)
        {
            return c == '(' || c == '[';
        }

        private Token ParseEndOfStatement(ref int i, ref int line, ref int offset)
        {
            var eos = new Token(TokenKind.EOS) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return eos;
        }

        private Token ParseBlockOpen(ref int i, ref int line, ref int offset)
        {
            var block = new Token(TokenKind.BlockOpen) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return block;
        }

        private Token ParseBlockClose(ref int i, ref int line, ref int offset)
        {
            var block = new Token(TokenKind.BlockClose) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return block;
        }

        private Token ParseSeparator(ref int i, ref int line, ref int offset)
        {
            var separator = new Token(TokenKind.Separator) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return separator;
        }

        private Token ParseBracketOpen(ref int i, ref int line, ref int offset)
        {
            var bracket = new Token(TokenKind.BracketOpen) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return bracket;
        }

        private Token ParseBracketClose(ref int i, ref int line, ref int offset)
        {
            var bracket = new Token(TokenKind.BracketClose) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return bracket;
        }

        private Token ParseDot(ref int i, ref int line, ref int offset)
        {
            var dot = new Token(TokenKind.Dot) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return dot;
        }

        private Token ParseColon(ref int i, ref int line, ref int offset)
        {
            var colon = new Token(TokenKind.Colon) { Input = input.Slice(i, 1), Line = line, Position = offset };

            i++;
            offset++;

            return colon;
        }


        private Token ParseComment(ref int i, ref int line, ref int position)
        {
            int start = i;
            int startPosition = position;

            for (; i < input.Length; i++)
            {
                var c = input.Span[i];
                if (c == newLineChar)
                {
                    break;
                }

                position++;
            }

            var result = new Token(TokenKind.Comment) { Input = input.Slice(start, i - start), Line = line, Position = startPosition };
            return result;
        }

        private Token ParseText(ref int i, char enterQuote, ref int line, ref int position)
        {
            int start = i;
            int startPosition = position;

            for (; i < input.Length; i++)
            {
                var c = input.Span[i];
                //
                // Enter and Exit Quotes have to match.
                //
                if (c == enterQuote)
                {
                    break;
                }

                position++;
            }

            var result = new Token(TokenKind.Text) { Input = input.Slice(start, i - start), Line = line, Position = startPosition };
            return result;
        }

        private Token ParseRange(ref int i, ref int line, ref int position)
        {
            int start = i;
            int startPosition = position;
            int count = 0;

            for (; i < input.Length; i++)
            {
                var c = input.Span[i];
                if (c != '.')
                {
                    break;
                }

                if (count > 2)
                    throw new ArgumentException($"Unexpected Token at: {i}");


                count++;
                position++;
            }

            if (count != 2)
            {
                throw new ArgumentException($"Unexpected Token at: {i}");
            }

            var result = new Token(TokenKind.Range) { Input = input.Slice(start, i - start), Line = line, Position = startPosition };
            return result;
        }

        private Token ParseCommand(ref int i, ref int line, ref int position)
        {
            int start = i;
            int startPosition = position;

            for (; i < input.Length; i++)
            {
                var c = input.Span[i];
                if (c == ';' || c == ' ')
                {
                    break;
                }

                position++;
            }
            //
            // A Command can have argumennts after it but it doesn't have to
            // so that's why when we expect EOF ';' we need to back up a char.
            //
            var result = new Token(TokenKind.Command) { Input = input.Slice(start, i - start), Line = line, Position = startPosition };
            return result;
        }

        private Token ParseKeywordOrVar(ref int i, ref int line, ref int position)
        {
            int start = i;
            int startPosition = position;

            //
            // Word has to start with letter, but after that we allow '_' and digits.
            //
            for (; i < input.Length; i++)
            {
                var c = input.Span[i];

                if ((char.IsLetter(c) == false && char.IsDigit(c) == false) && c != '_')
                {
                    break;
                }

                position++;
            }

            var result = new Token(TokenKind.Word) { Input = input.Slice(start, i - start), Line = line, Position = startPosition };
            return result;
        }

        private Token ParseBinaryOperator(ref int i, ref int line, ref int position)
        {
            int level = 0;
            int start = i;
            int startPosition = position;

            for (; i < input.Length; i++)
            {
                var c = input.Span[i];

                if (IsBinaryOperator(i) == false && IsChar(i, '=') == false)
                {
                    break;
                }
                else
                {
                    switch (c)
                    {
                        case '~': level = 40; break;
                        case '^': level = 40; break;
                        case '&': level = 40; break;
                        case '|': level = 40; break;
                        case '>': level = 50; break;
                        case '<': level = 50; break;
                        case '!': level = 50; break;
                        case '=': level = 50; break;
                    }
                }

                position++;
            }

            var result = new BinaryOperatorToken() { Input = input.Slice(start, i - start), Level = level, Line = line, Position = startPosition };

            return result;
        }

        private Token ParseOperator(ref int i, ref int line, ref int position)
        {
            int level = 0;
            int start = i;
            int startPosition = position;

            for (; i < input.Length; i++)
            {
                var c = input.Span[i];

                if (IsOperator(i) == false)
                {
                    break;
                }
                else
                {
                    switch (c)
                    {
                        case '-': level = 100; break;
                        case '+': level = 100; break;
                        case '*': level = 200; break;
                        case '/': level = 200; break;
                        case '%': level = 200; break;
                    }
                }

                position++;

            }

            var result = new OperatorToken() { Input = input.Slice(start, i - start), Level = level, Line = line, Position = startPosition };
            return result;
        }

        public NumberToken ParseNumber(ref int i, ref int line, ref int position)
        {
            int start = i;
            int startPosition = position;
            char format = 'n';
            bool signed = true;

            if (i + 1 < input.Length && input.Span[i] == '0' &&
                (input.Span[i + 1] == 'x' || input.Span[i + 1] == 'b'))
            {
                format = input.Span[i + 1];
                position += 2;
                i += 2;
                start = i;
            }

            for (; i < input.Length; i++)
            {
                var c = input.Span[i];

                if (char.IsWhiteSpace(c))
                    break;

                if (char.IsNumber(c) == false && c != '_' && (format == 'x' && hexTable.Contains(c)) == false)
                {
                    break;
                }

                position++;
            }
            //
            // Check sign
            //
            if (i < input.Length && (input.Span[i] == 'u' || input.Span[i] == 'U'))
            {
                signed = false;
                position++;
                i++;
            }

            var result = new NumberToken()
            {
                IsSigned = signed,
                Format = format,
                Input = input.Slice(start, signed == false ? (i - 1) - start : i - start),
                Line = line,
                Position = startPosition
            };
            return result;
        }
    }

    public enum TokenKind
    {
        Number,
        Word,
        Separator,
        Range,
        Comment,
        Text,
        Colon,
        Operator,
        BinaryOperator,
        EOS,
        Dot,
        BlockOpen,
        BlockClose,
        BracketOpen,
        BracketClose,
        Command
    }

    public class Token
    {
        public Token(TokenKind kind)
        {
            Kind = kind;
        }

        public int Line { get; set; }
        public int Position { get; set; }

        public TokenKind Kind { get; set; }
        public ReadOnlyMemory<char> Input { get; set; }

        private string value;

        public string GetValue()
        {
            if (value == null)
                value = Input.ToString();

            return value;
        }
    }

    public class NumberToken : Token
    {
        public char Format { get; set; }
        public bool IsSigned { get; set; }
        public NumberToken() : base(TokenKind.Number) { }
    }

    public class OperatorToken : Token
    {
        public int Level { get; set; }

        public OperatorToken() : base(TokenKind.Operator) { }
        public OperatorToken(TokenKind kind) : base(kind) { }
    }

    public class BinaryOperatorToken : OperatorToken
    {
        public BinaryOperatorToken() : base(TokenKind.BinaryOperator) { }
    }
}
