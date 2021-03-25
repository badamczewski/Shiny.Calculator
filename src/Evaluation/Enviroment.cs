using Shiny.Calculator.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiny.Calculator.Evaluation
{
    //
    // Enviroment controls lexical scopes.
    //
    public class Enviroment
    {
        public string Name { get; set; }

        public Dictionary<string, EvaluatorState> Variables = new Dictionary<string, EvaluatorState>()
        {
            { "res", new EvaluatorState() { Value = null, Type = LiteralType.Any } }
        };

        public int StatementIndex = 0;
        public Dictionary<string, int> Labels = new Dictionary<string, int>();
    }
}
