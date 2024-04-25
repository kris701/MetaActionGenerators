using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDDLSharp.Toolkit.MacroGenerators;
using PDDLSharp.Models.FastDownward.Plans;
using MetaActionGenerators.Helpers;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Parsers.FastDownward.Plans;
using PDDLSharp.Parsers.PDDL;
using PDDLSharp.Parsers;
using MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction;
using PDDLSharp.CodeGenerators.PDDL;

namespace MetaActionGenerators.CandidateGenerators.CSMMacrosMetaAction
{
    public class PDDLSharpMutexedMetaActions : BaseCandidateGenerator
    {
        public override Dictionary<string, string> GeneratorArgs { get; internal set; } = new Dictionary<string, string>()
        {
            { "macroLimit", "" },
            { "tempFolder", "" },
            { "fastDownwardPath", "" },
            { "logFD", "false" }
        };

        public PDDLSharpMutexedMetaActions(Dictionary<string, string> generatorArgs, PDDLDecl decl) : base(decl)
        {
            HandleArgs(generatorArgs);
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            if (!Directory.Exists(GeneratorArgs["tempFolder"]))
                Directory.CreateDirectory(GeneratorArgs["tempFolder"]);
            var macroGenerator = new SequentialMacroGenerator(Decl);
            var macros = macroGenerator.FindMacros(GetPlans(), int.Parse(GeneratorArgs["macroLimit"]));
            var candidates = new List<ActionDecl>();

            foreach (var macro in macros) 
            {
                var reducer = new MacroReductionMetaActions(Decl, macro);
                candidates.AddRange(reducer.GenerateCandidates());
            }

            return candidates;
        }

        private List<ActionPlan> GetPlans()
        {
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var planParser = new FDPlanParser(listener);
            var plans = new List<ActionPlan>();
            var codeGenerator = new PDDLCodeGenerator(listener);
            var domain = Path.Combine(GeneratorArgs["tempFolder"], "tempDomain.pddl");
            codeGenerator.Generate(Decl.Domain, domain);
            var problem = Path.Combine(GeneratorArgs["tempFolder"], "tempProblem.pddl");
            codeGenerator.Generate(Decl.Problem, problem);

            var doLog = GeneratorArgs["logFD"] == "true";
            using (ArgsCaller fdCaller = new ArgsCaller("python3"))
            {
                fdCaller.StdOut += (s, o) => {
                    if (doLog)
                        Console.WriteLine(o.Data);
                };
                fdCaller.StdErr += (s, o) => {
                    if (doLog)
                        Console.WriteLine(o.Data);
                };
                fdCaller.Arguments.Add(GeneratorArgs["fastDownwardPath"], "");
                fdCaller.Arguments.Add("--alias", "lama-first");
                fdCaller.Arguments.Add("--overall-time-limit", "5m");
                fdCaller.Arguments.Add("--plan-file", "plan.plan");
                fdCaller.Arguments.Add("tempDomain.pddl", "");
                fdCaller.Arguments.Add("tempProblem.pddl", "");
                fdCaller.Process.StartInfo.WorkingDirectory = GeneratorArgs["tempFolder"];
                if (fdCaller.Run() != 0)
                    throw new Exception("Fast downward failed!");
                plans.Add(planParser.Parse(new FileInfo(Path.Combine(GeneratorArgs["tempFolder"], "plan.plan"))));
            }

            return plans;
        }
    }
}
