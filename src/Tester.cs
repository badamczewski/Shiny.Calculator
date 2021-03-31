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
    public class Tester
    {
        public void RunLanguageTests(string path)
        {
            XConsole.WriteLine("Running Language Tests:", Colors.Green);
            int padding = 80;

            foreach (var langTest in Directory.GetFiles(path))
            {
                XConsole.Write(" " + Path.GetFileName(langTest), Colors.White);


                Tokenizer tokenizer = new Tokenizer();
                Parser parser = new Parser(Definitions.Commands, Definitions.Operators, Definitions.Instructions);
                Evaluator evaluator = new Evaluator();
                NullPrinter printer = new NullPrinter();
                VariableAndContextResolver variableAndContextResolver = new VariableAndContextResolver();

                int pad = padding - langTest.Length;
                if (pad <= 0) pad = 1;

                XConsole.Write(new string('.', pad), Colors.White);

                AST ast = null;
                EvaluatorState result = null;
                try
                {
                    var test = File.ReadAllText(langTest);
                    var tokens = tokenizer.Tokenize(test);
                    ast = parser.Parse(tokens);
                    variableAndContextResolver.Resolve(ast, printer, out var resolved);
                    result = evaluator.Evaluate(ast, resolved, printer, new EvaluatorContext());
                }
                catch { }

                if (result == null)
                {
                    XConsole.WriteLine("[ERROR / EXCEPTION]", Colors.Red);
                }
                else if(result is ErrorState error)
                {
                    XConsole.WriteLine("[ERROR / EVAL]", Colors.Red);
                }
                else if(ast.Errors.Any())
                {
                    XConsole.WriteLine("[ERROR / PARSE]", Colors.Red);
                }
                else
                {
                    XConsole.WriteLine("[OK]", Colors.Green);
                }

            }
        }
    }

    
}
