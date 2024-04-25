using MetaActionGenerators.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using System.Data;
using System.Diagnostics;

namespace MetaActionGenerators.CandidateGenerators
{
    public class CPDDLMutexedMetaActions : BaseCandidateGenerator
    {
        public override Dictionary<string, string> GeneratorArgs { get; internal set; } = new Dictionary<string, string>()
        {
            { "cpddlExecutable", "" },
            { "tempFolder", "" }
        };

        public CPDDLMutexedMetaActions(Dictionary<string, string>  generatorArgs, PDDLDecl decl) : base(decl)
        {
            var toSet = generatorArgs.Keys.ToList();
            foreach (var key in generatorArgs.Keys)
            {
                if (GeneratorArgs.ContainsKey(key))
                {
                    GeneratorArgs[key] = generatorArgs[key];
                    toSet.Remove(key);
                }
            }
            if (toSet.Count > 0)
                throw new Exception($"Missing argument: {toSet[0]}");
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            if (!File.Exists(GeneratorArgs["cpddlExecutable"]))
                throw new FileNotFoundException($"Could not find the file: {GeneratorArgs["cpddlExecutable"]}");
            if (Decl.Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");

            var rules = ExecuteCPDDL(Decl);

            var candidates = new List<ActionDecl>();
            foreach (var predicate in Decl.Domain.Predicates.Predicates)
            {
                if (!Statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
                {
                    bool invarianted = rules.Any(x => x.Any(y => y.Predicate == predicate.Name));
                    if (invarianted)
                        candidates.AddRange(GeneateInvariantSafeCandidates(rules, Decl, predicate));
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

            return candidates.Distinct(Decl.Domain.Actions);
        }

        private List<List<PredicateRule>> ExecuteCPDDL(PDDLDecl pddlDecl)
        {
            PathHelper.RecratePath(GeneratorArgs["tempFolder"]);
            var codeGenerator = new PDDLCodeGenerator(new ErrorListener());
            codeGenerator.Generate(pddlDecl.Domain, Path.Combine(GeneratorArgs["tempFolder"], "domain.pddl"));
            codeGenerator.Generate(pddlDecl.Problem, Path.Combine(GeneratorArgs["tempFolder"], "problem.pddl"));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = GeneratorArgs["cpddlExecutable"],
                    Arguments = "--lmg-out output.txt --lmg-stop domain.pddl problem.pddl",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = GeneratorArgs["tempFolder"]
                }
            };
            process.Start();
            process.WaitForExit();

            if (!File.Exists(Path.Combine(GeneratorArgs["tempFolder"], "output.txt")))
                return new List<List<PredicateRule>>();

            var rules = new List<List<PredicateRule>>();

            foreach (var line in File.ReadLines(Path.Combine(GeneratorArgs["tempFolder"], "output.txt")))
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
            foreach(var ruleSet in rules)
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

        private class Candidate
        {
            public List<IExp> Preconditions { get; set; }
            public List<IExp> Effects { get; set; }

            public Candidate(List<IExp> preconditions, List<IExp> effects)
            {
                Preconditions = preconditions;
                Effects = effects;
            }

            public Candidate Copy()
            {
                var preconditions = new List<IExp>();
                foreach (var precon in Preconditions)
                    preconditions.Add((IExp)precon.Copy());
                var effects = new List<IExp>();
                foreach (var effect in Effects)
                    effects.Add((IExp)effect.Copy());

                return new Candidate(preconditions, effects);
            }
        }

        private class PredicateRule
        {
            public string Predicate { get; set; }
            public List<string> Args { get; set; }

            public PredicateRule(string predicate, List<string> args)
            {
                Predicate = predicate;
                Args = args;
            }

            public override string ToString()
            {
                var args = "";
                foreach (var arg in Args)
                    args += $"{arg}, ";
                args = args.Trim();
                if (args.EndsWith(','))
                    args = args.Substring(0, args.Length - 1);
                return $"{Predicate}: {args}";
            }
        }
    }
}