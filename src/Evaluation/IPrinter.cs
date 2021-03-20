using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using BinaryExpression = Shiny.Calculator.Parsing.BinaryExpression;
using Expression = Shiny.Calculator.Parsing.AST_Node;
using UnaryExpression = Shiny.Calculator.Parsing.UnaryExpression;

namespace Shiny.Calculator.Evaluation
{
    public interface IPrinter
    {
        int Indent { get; set; }

        void PrintInline(params Run[] runs);
        void Print(params Run[] runs);
        void Clear();
    }

  
}
