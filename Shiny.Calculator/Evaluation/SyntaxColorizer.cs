using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace Shiny.Calculator.Evaluation
{
    public enum SyntaxRunType
    {
        Unknown,
        Keyword,
        Identifier,
        String
    }

    public class SyntaxRun
    {
        public SyntaxRunType Type { get; set; }
        public string Value { get; set; }
    }

    public class CodeColorizer
    {
        private IPrinter _printer;

        public CodeColorizer(IPrinter printer)
        {
            this._printer = printer;
        }

        public void Colorize(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var walker = new SyntaxRunWalker();
            walker.Visit(tree.GetRoot());
            var syntax = walker.GetSyntax();

            foreach (var s in syntax)
            {
                Color runColor = Colors.Gray;

                if (s.Type == SyntaxRunType.Keyword)
                    runColor = Colors.Blue;
                else if (s.Type == SyntaxRunType.Identifier)
                    runColor = Colors.White;
                else if (s.Type == SyntaxRunType.String)
                    runColor = Colors.Gray;
                else
                    runColor = Colors.DarkGray;

                _printer.PrintInline(new Run() { Color = runColor, Text = s.Value });
            }

            _printer.Print();

        }

        public class SyntaxRunWalker : SyntaxWalker
        {
            private readonly List<SyntaxRun> _result = new List<SyntaxRun>();

            public SyntaxRunWalker() : base(SyntaxWalkerDepth.StructuredTrivia) { }

            public List<SyntaxRun> GetSyntax()
            {
                return _result;
            }

            protected override void VisitToken(SyntaxToken token)
            {
                ProcessTrivia(token.LeadingTrivia);

                if (token.IsKeyword())
                {
                    _result.Add(new SyntaxRun() { Type = SyntaxRunType.Keyword, Value = token.ToString() });
                }
                else
                {
                    if (token.Kind() == SyntaxKind.IdentifierToken)
                    {
                        _result.Add(new SyntaxRun() { Type = SyntaxRunType.Identifier, Value = token.ToString() });
                    }
                    else if (token.Kind() == SyntaxKind.StringLiteralToken)
                    {
                        _result.Add(new SyntaxRun() { Type = SyntaxRunType.String, Value = token.ToString() });
                    }
                    else
                    {
                        _result.Add(new SyntaxRun() { Value = token.ToString() });
                    }
                }

                ProcessTrivia(token.TrailingTrivia);

                base.VisitToken(token);
            }

            private void ProcessTrivia(SyntaxTriviaList list)
            {
                foreach (var trivia in list)
                {
                    if (trivia.Kind() != SyntaxKind.WarningDirectiveTrivia)
                    {
                        _result.Add(new SyntaxRun() { Value = trivia.ToString() });
                    }
                }
            }
        }
    }
}
