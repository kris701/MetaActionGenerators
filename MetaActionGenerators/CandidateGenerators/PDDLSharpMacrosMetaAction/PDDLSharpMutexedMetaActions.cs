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
using PDDLSharp.Models.PDDL.Problem;

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

        public PDDLSharpMutexedMetaActions(Dictionary<string, string> generatorArgs, DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
            HandleArgs(generatorArgs);
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            PathHelper.RecratePath(GeneratorArgs["tempFolder"]);
            var macroGenerator = new SequentialMacroGenerator(new PDDLDecl(Domain, Problems[0]));
            var macros = macroGenerator.FindMacros(GetPlans(), int.Parse(GeneratorArgs["macroLimit"]));
            var candidates = new List<ActionDecl>();

            foreach (var macro in macros) 
            {
                var reducer = new MacroReductionMetaActions(Domain, Problems, macro);
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

            var domainFile = Path.Combine(GeneratorArgs["tempFolder"], "tempDomain.pddl");
            codeGenerator.Generate(Domain, domainFile);
            foreach (var problem in Problems)
            {
                var problemFile = Path.Combine(GeneratorArgs["tempFolder"], "tempProblem.pddl");
                codeGenerator.Generate(problem, problemFile);

                var doLog = GeneratorArgs["logFD"] == "true";
                using (ArgsCaller fdCaller = new ArgsCaller("python3"))
                {
                    fdCaller.StdOut += (s, o) =>
                    {
                        if (doLog)
                            Console.WriteLine(o.Data);
                    };
                    fdCaller.StdErr += (s, o) =>
                    {
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
            }

            return plans;
        }
    }
}
