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
            if (Args.GetArgument<string>("cpddlOutput") != "" && !File.Exists(Args.GetArgument<string>("cpddlOutput")))
                throw new FileNotFoundException($"Could not find the CPDDL Output file: {Args.GetArgument<string>("cpddlOutput")}");
            else if (!File.Exists(Args.GetArgument<string>("cpddlExecutable")))
                throw new FileNotFoundException($"Could not find the file: {Args.GetArgument<string>("cpddlExecutable")}");

            var decl = new PDDLDecl(Domain, Problems[LargestProblem()]);
            var cpddlOut = "";
            if (Args.GetArgument<string>("cpddlOutput") != "")
                cpddlOut = File.ReadAllText(Args.GetArgument<string>("cpddlOutput"));
            else
                cpddlOut = ExecuteCPDDL(decl);
            var rules = ParseCPDDLOutput(cpddlOut);

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

        private List<List<PredicateRule>> ParseCPDDLOutput(string text)
        {
            var rules = new List<List<PredicateRule>>();

            var lines = text.Split(Environment.NewLine);
            foreach (var line in lines)
            {
                if (line.EndsWith(":=1"))
                {
                    var inner = line.Substring(line.IndexOf('{') + 1, line.IndexOf('}') - line.IndexOf('{') - 1);
                    var subRules = new List<PredicateRule>();
                    var subRulesStr = inner.Split(',');
                    bool valid = true;
                    foreach (var subRule in subRulesStr)
                    {
                        var subRuleStr = subRule.Trim();
                        var predName = subRuleStr;
                        if (subRuleStr.Contains(' '))
                            predName = subRuleStr.Substring(0, subRuleStr.IndexOf(' '));
                        if (predName.StartsWith("NOT-"))
                        {
                            valid = false;
                            break;
                        }
                        var fixArgs = new List<string>();
                        if (subRuleStr.Contains(' '))
                        {
                            var args = subRuleStr.Substring(subRuleStr.IndexOf(' ')).Split(' ').ToList();
                            args.RemoveAll(x => x == "");
                            for (int i = 0; i < args.Count; i++)
                            {
                                if (args[i].StartsWith('C'))
                                    fixArgs.Add(args[i].Substring(0, args[i].IndexOf(':')));
                                else if (args[i].StartsWith('V'))
                                    fixArgs.Add(args[i].Substring(0, args[i].IndexOf(':')));
                                else
                                    valid = false;
                            }
                            if (!valid)
                                break;
                        }
                        subRules.Add(new PredicateRule(predName, fixArgs));
                    }

                    if (valid)
                        rules.Add(subRules);
                }
            }

            // Make sure rule argument IDs are unique 
            var index = 0;
            foreach (var ruleSet in rules)
            {
                foreach (var rule in ruleSet)
                    for (int i = 0; i < rule.Args.Count; i++)
                        if (rule.Args[i].StartsWith('C'))
                            rule.Args[i] = $"C{index}_{rule.Args[i].Substring(1)}";
                index++;
            }

            rules = rules.OrderBy(x => x.Count).ToList();

            return rules;
        }

        private List<ActionDecl> GeneateInvariantSafeCandidates(List<List<PredicateRule>> rules, PDDLDecl pddlDecl, PredicateExp predicate)
        {
            var candidateOptions = InvGen(rules, new Candidate(new List<IExp>(), new List<IExp>() { predicate }), pddlDecl.Domain, new List<List<PredicateRule>>());
            int version = 0;
            var candidates = new List<ActionDecl>();
            foreach (var option in candidateOptions)
                candidates.Add(GenerateMetaAction(
                    $"meta_{predicate.Name}_{version++}",
                    option.Preconditions,
                    option.Effects));

            return candidates;
        }

        private List<Candidate> InvGen(List<List<PredicateRule>> rules, Candidate candidate, DomainDecl domain, List<List<PredicateRule>> covered)
        {
            var coveredNow = new List<List<PredicateRule>>(covered);
            for (int i = 0; i < candidate.Effects.Count; i++)
            {
                var reference = GetReferencePredicate(candidate.Effects[i]);

                foreach (var ruleSet in rules)
                {
                    if (coveredNow.Contains(ruleSet))
                        continue;
                    if (!ruleSet.Any(x => x.Predicate == reference.Name))
                        continue;

                    if (ruleSet.Count == 1)
                    {
                        var target = GetMutatedBinaryIExp(candidate.Effects[i], ruleSet[0], ruleSet[0], domain);
                        if (!candidate.Effects.Contains(target))
                        {
                            AddTargetToCandidate(candidate, target);
                            i = -1;
                            coveredNow.Add(ruleSet);
                            break;
                        }
                    }
                    else
                    {
                        coveredNow.Add(ruleSet);
                        var candidates = new List<Candidate>();
                        var sourceRule = GetMatchingRule(candidate.Effects[i], ruleSet);
                        var others = ruleSet.Where(x => x != sourceRule);
                        foreach (var targetRule in others)
                        {
                            var target = GetMutatedBinaryIExp(candidate.Effects[i], sourceRule, targetRule, domain);
                            if (!candidate.Effects.Contains(target))
                            {
                                var cpy = candidate.Copy();
                                AddTargetToCandidate(cpy, target);
                                candidates.AddRange(InvGen(rules, cpy, domain, coveredNow));
                            }
                        }
                        return candidates;
                    }
                }
            }
            return new List<Candidate> { candidate };
        }

        private void AddTargetToCandidate(Candidate candidate, IExp target)
        {
            candidate.Effects.Add(target);
            if (target is NotExp not)
                candidate.Preconditions.Add(not.Child);
            else
                candidate.Preconditions.Add(GenerateNegated(target));
        }

        private PredicateRule GetMatchingRule(IExp exp, List<PredicateRule> rules)
        {
            var predicate = GetReferencePredicate(exp);
            return rules.First(x => x.Predicate == predicate.Name);
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