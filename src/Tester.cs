using Shiny.Calculator.Evaluation;
using Shiny.Calculator.Parsing;
using Shiny.Calculator.Tokenization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiny.Calculator
{
    public class FullTest
    {
        private Tokenizer tokenizer = new Tokenizer();
        private Parser parser = new Parser(Definitions.Commands, Definitions.Operators, Definitions.Instructions);
        private Evaluator evaluator = new Evaluator();
        private ConsolePrinter printer = new ConsolePrinter();

        public bool T001_SimpleExpression()
        {
            //
            // Test
            //
            var test = "1 + 2";
            string expected = "3";

            //
            // Setup
            //
            EvaluatorContext context = new EvaluatorContext();
            VariableAndContextResolver resolver = new VariableAndContextResolver();

            var tokens = tokenizer.Tokenize(test);
            var ast = parser.Parse(tokens);

            //
            // Expected 
            //
            if (resolver.Resolve(ast, printer, out var resolved))
            {
                var result = evaluator.Evaluate(ast, resolved, printer, context);
                return result.Value == expected;
            }

            return false;
        }
    }

    public class Tester
    {
        public void RunTests()
        {
            
        }
    }

    
}
