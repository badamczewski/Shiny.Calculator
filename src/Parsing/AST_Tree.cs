using Shiny.Calculator.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiny.Calculator.Parsing
{
    public class AST
    {
        public List<AST_Node> Statements { get; set; }
        public IEnumerable<AST_Error> Errors { get; set; }
    }
}
