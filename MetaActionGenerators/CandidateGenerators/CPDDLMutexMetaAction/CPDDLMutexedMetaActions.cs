using MetaActionGenerators.ArgumentSystem;
using MetaActionGenerators.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;
using System.Data;
using System.Diagnostics;

namespace MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction
{
    public class CPDDLMutexedMetaActions : BaseCandidateGenerator
    {
        public CPDDLMutexedMetaActions(Dictionary<string, string> generatorArgs, DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
            Args = new ArgsHandler(new List<Arg>()
            {
                new Arg("cpddlExecutable", "", "Path to a compiled binary of CPDDL, this should be the /bin/pddl file in the CPDDL repository."),
                new Arg("tempFolder", "", "A path to a folder to store temporary files from the CPDDL execution."),
                new Arg("cpddlOutput", "", "A path to the output of a CPDDL run (if you dont want to run the tool at runtime)")
            }, generatorArgs);
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            if (Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");
            if (Args.GetArgument<string>("cpddlOutput") != "")
            {
                if (!File.Exists(Args.GetArgument<string>("cpddlOutput")))
                    throw new FileNotFoundException($"Could not find the CPDDL Output file: {Args.GetArgument<string>("cpddlOutput")}");
            }
            else if (!File.Exists(Args.GetArgument<string>("cpddlExecutable")))
                throw new FileNotFoundException($"Could not find the CPDDL executable file: {Args.GetArgument<string>("cpddlExecutable")}");

            var decl = new PDDLDecl(Domain, Problems[LargestProblem()]);
            var cpddlOut = "";
            if (Args.GetArgument<string>("cpddlOutput") != "")
                cpddlOut = File.ReadAllText(Args.GetArgument<string>("cpddlOutput"));
            else
                cpddlOut = ExecuteCPDDL(decl);
            var rules = CPDDLParser.ParseCPDDLOutput(cpddlOut);

            var candidates = new List<ActionDecl>();
            foreach (var predicate in Domain.Predicates.Predicates)
            {
                if (!Statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
                {
                    bool invarianted = rules.Any(x => x.Any(y => y.Predicate == predicate.Name));
                    if (invarianted)
                        candidates.AddRange(GeneateInvariantSafeCandidates(rules, decl, predicate));
                    else
                    {
                        candidates.Add(GenerateMetaAction(
                            $"meta_{predicate.Name}",
                            new List<IExp>(),
                            new List<IExp>() { predicate }));
                        candidates.Add(GenerateMetaAction(
                            $"meta_{predicate.Name}_false",
                            new List<IExp>(),
                            new List<IExp>() { new NotExp(predicate) }));
                    }
                }
            }

            return candidates.Distinct(Domain.Actions);
        }

        private int LargestProblem()
        {
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());
            int largestIndex = -1;
            int largestSize = -1;

            for (int i = 0; i < Problems.Count; i++)
            {
                var text = codeGenerator.Generate(Problems[i]);
                if (text.Length > largestSize)
                {
                    largestSize = text.Length;
                    largestIndex = i;
                }
            }

            return largestIndex;
        }

        private string ExecuteCPDDL(PDDLDecl pddlDecl)
        {
            PathHelper.RecratePath(Args.GetArgument<string>("tempFolder"));
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());
            codeGenerator.Generate(pddlDecl.Domain, Path.Combine(Args.GetArgument<string>("tempFolder"), "domain.pddl"));
            codeGenerator.Generate(pddlDecl.Problem, Path.Combine(Args.GetArgument<string>("tempFolder"), "problem.pddl"));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Args.GetArgument<string>("cpddlExecutable"),
                    Arguments = "--lmg-out output.txt --lmg-stop domain.pddl problem.pddl",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Args.GetArgument<string>("tempFolder")
                }
            };
            process.Start();
            process.WaitForExit();

            if (!File.Exists(Path.Combine(Args.GetArgument<string>("tempFolder"), "output.txt")))
                return "";
            return File.ReadAllText(Path.Combine(Args.GetArgument<string>("tempFolder"), "output.txt"));
        }

        private List<ActionDecl> GeneateInvariantSafeCandidates(List<List<PredicateRule>> rules, PDDLDecl pddlDecl, PredicateExp predicate)
        {
            var candidateOptions = UpholdAll(new Candidate(new List<IExp>(), new Dictionary<IExp, List<int>>() { { predicate, new List<int>() } }), rules, pddlDecl.Domain);
            candidateOptions = candidateOptions.Distinct().ToList();
            int version = 0;
            var candidates = new List<ActionDecl>();
            foreach (var option in candidateOptions)
                candidates.Add(GenerateMetaAction(
                    $"meta_{predicate.Name}_{version++}",
                    option.Preconditions,
                    option.Effects.Keys.ToList()));

            return candidates;
        }

        private List<Candidate> UpholdAll(Candidate c, List<List<PredicateRule>> G, DomainDecl domain)
        {
            var R = new List<Candidate>() { c };

            var preCount = 0;
            while (preCount != R.Count)
            {
                preCount = R.Count;
                var index = 0;
                foreach (var g in G)
                {
                    var newR = new List<Candidate>();
                    foreach (var r in R)
                        newR.AddRange(Uphold(r, g, domain, index));
                    R = newR.Distinct().ToList();
                    index++;
                }
            }

            return R;
        }

        private List<Candidate> Uphold(Candidate c, List<PredicateRule> g, DomainDecl domain, int index)
        {
            var R = new List<Candidate>();

            foreach (var e in c.Effects)
            {
                if (e.Value.Contains(index))
                    continue;

                if (e.Key is NotExp)
                    continue;
                var reference = GetReferencePredicate(e.Key);
                if (!g.Any(x => x.Predicate == reference.Name))
                    continue;

                e.Value.Add(index);

                if (g.Count == 1)
                {
                    var target = GetMutatedBinaryIExp(e.Key, g[0], g[0], domain);
                    if (!ContainsOrIsInvalid(target, c.Effects.Keys.ToList()))
                    {
                        var cpy = c.Copy();
                        AddTargetToCandidate(cpy, target, index);
                        R.Add(cpy);
                    }
                }
                else
                {
                    var sourceRule = g.First(x => x.Predicate == reference.Name);
                    var others = g.Where(x => x != sourceRule);
                    foreach (var otherRule in others)
                    {
                        var target = GetMutatedBinaryIExp(e.Key, sourceRule, otherRule, domain);
                        if (!ContainsOrIsInvalid(target, c.Effects.Keys.ToList()))
                        {
                            var cpy = c.Copy();
                            AddTargetToCandidate(cpy, target, index);
                            R.Add(cpy);
                        }
                    }
                }
            }

            if (R.Count == 0)
                R.Add(c);

            return R;
        }

        private bool ContainsOrIsInvalid(IExp exp, List<IExp> effects)
        {
            if (effects.Contains(exp))
                return true;
            if (exp is NotExp not && effects.Contains(not.Child))
                return true;
            if (effects.Contains(new NotExp(exp)))
                return true;
            return false;
        }

        private void AddTargetToCandidate(Candidate candidate, IExp target, int index)
        {
            candidate.Effects.Add(target, new List<int>() { index });
            if (target is NotExp not)
                candidate.Preconditions.Add(not.Child);
            else
                candidate.Preconditions.Add(GenerateNegated(target));
        }

        private IExp GetMutatedBinaryIExp(IExp exp, PredicateRule sourceRule, PredicateRule targetRule, DomainDecl domain)
        {
            var reference = GetReferencePredicate(exp);

            var sourceRuleArgs = new List<string>(sourceRule.Args);
            var targetRuleArgs = new List<string>(targetRule.Args);
            var sample = domain.Predicates!.Predicates.First(x => x.Name == targetRule.Predicate).Copy();
            for (int i = 0; i < targetRuleArgs.Count; i++)
            {
                if (targetRuleArgs[i].StartsWith('V'))
                    sample.Arguments[i].Name = reference.Arguments[sourceRuleArgs.IndexOf(targetRuleArgs[i])].Name;
                else
                    sample.Arguments[i].Name = $"?{targetRuleArgs[i]}";
            }

            if (exp is PredicateExp)
                return GenerateNegated(sample);
            else if (exp is NotExp not2 && not2.Child is PredicateExp)
                return sample;
            throw new Exception();
        }

        private PredicateExp GetReferencePredicate(IExp exp)
        {
            if (exp is PredicateExp pred)
                return pred.Copy();
            else if (exp is NotExp not && not.Child is PredicateExp pred2)
                return pred2.Copy();
            else
                throw new ArgumentNullException("Impossible mutation generation");
        }

        private NotExp GenerateNegated(IExp exp)
        {
            var newNot = new NotExp(exp);
            newNot.Child.Parent = newNot;
            return newNot;
        }
    }
}