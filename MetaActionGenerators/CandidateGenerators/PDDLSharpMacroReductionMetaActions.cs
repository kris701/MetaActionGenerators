using MetaActionGenerators.ArgumentSystem;
using MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction;
using MetaActionGenerators.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.FastDownward.Plans;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using PDDLSharp.Parsers.PDDL;
using PDDLSharp.Toolkit.MacroGenerators;

namespace MetaActionGenerators.CandidateGenerators
{
    public class PDDLSharpMacroReductionMetaActions : BaseCandidateGenerator
    {
        public PDDLSharpMacroReductionMetaActions(Dictionary<string, string> generatorArgs, DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
            Args = new ArgsHandler(new List<Arg>()
            {
                new Arg("macroLimit", 10, "An integer limit to how many macros PDDLSharp should make."),
                new Arg("tempFolder", "A folder to store temporary files."),
                new Arg("fastDownwardPath", "A path to a build of Fast Downward. This should be to the `fast-downward.py` file."),
                new Arg("logFD", false, "Output the stdout from Fast Downward into the console for debug purposes.")
            }, generatorArgs);
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            PathHelper.RecratePath(Args.GetArgument<string>("tempFolder"));
            var macroGenerator = new SequentialMacroGenerator(new PDDLDecl(Domain, Problems[0]));
            var macros = macroGenerator.FindMacros(GetPlans(), Args.GetArgument<int>("macroLimit"));
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

            var domainFile = Path.Combine(Args.GetArgument<string>("tempFolder"), "tempDomain.pddl");
            codeGenerator.Generate(Domain, domainFile);
            foreach (var problem in Problems)
            {
                var problemFile = Path.Combine(Args.GetArgument<string>("tempFolder"), "tempProblem.pddl");
                codeGenerator.Generate(problem, problemFile);

                var doLog = Args.GetArgument<bool>("logFD");
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
                    fdCaller.Arguments.Add(Args.GetArgument<string>("fastDownwardPath"), "");
                    fdCaller.Arguments.Add("--alias", "lama-first");
                    fdCaller.Arguments.Add("--overall-time-limit", "5m");
                    fdCaller.Arguments.Add("--plan-file", "plan.plan");
                    fdCaller.Arguments.Add("tempDomain.pddl", "");
                    fdCaller.Arguments.Add("tempProblem.pddl", "");
                    fdCaller.Process.StartInfo.WorkingDirectory = Args.GetArgument<string>("tempFolder");
                    if (fdCaller.Run() != 0)
                        throw new Exception("Fast downward failed!");
                    plans.Add(planParser.Parse(new FileInfo(Path.Combine(Args.GetArgument<string>("tempFolder"), "plan.plan"))));
                }
            }

            return plans;
        }
    }
}
