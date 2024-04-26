using MetaActionGenerators.ArgumentSystem;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Translators.Tools;

namespace MetaActionGenerators.CandidateGenerators
{
    public abstract class BaseCandidateGenerator : ICandidateGenerator
    {
        public virtual List<Arg> Args { get; } = new List<Arg>();
        public DomainDecl Domain { get; }
        public List<ProblemDecl> Problems { get; }

        public List<PredicateExp> Statics = new List<PredicateExp>();
        public List<PredicateExp> SimpleStatics = new List<PredicateExp>();

        protected BaseCandidateGenerator(DomainDecl domain, List<ProblemDecl> problems)
        {
            Domain = domain;
            Problems = problems;
            Statics = SimpleStaticPredicateDetector.FindStaticPredicates(new PDDLDecl(domain, problems[0]));
            Statics.Add(new PredicateExp("="));
            SimpleStatics = new List<PredicateExp>(Statics.Count);
            foreach (var staticItem in Statics)
                if (staticItem.Arguments.Count <= 1)
                    SimpleStatics.Add(staticItem);
        }

        internal T GetArgument<T>(string key)
        {
            var target = Args.FirstOrDefault(x => x.Key == key);
            if (target == null)
                throw new ArgumentNullException($"No argument with the key '{key}'!");
            if (target.Value == null)
                throw new ArgumentNullException($"Argument '{key}' was not set!");
            return (T)Convert.ChangeType(target.Value, typeof(T));
        }

        internal void HandleArgs(Dictionary<string, string> generatorArgs)
        {
            var toSet = Args.Where(x => x.Value == null).ToList();
            foreach (var key in generatorArgs.Keys)
            {
                var target = Args.FirstOrDefault(x => x.Key == key);
                if (target != null)
                {
                    target.Value = generatorArgs[key];
                    toSet.RemoveAll(x => x.Key == key);
                }
            }
            if (toSet.Count > 0)
                throw new Exception($"Missing argument: '{toSet[0].Key}', {toSet[0].Description}");
        }

        public List<ActionDecl> GenerateCandidates()
        {
            var candidates = GenerateCandidatesInner();
            foreach (var candidate in candidates)
                if (!candidate.Name.Contains('$'))
                    candidate.Name = $"${candidate.Name}";
            while (candidates.DistinctBy(x => x.Name).Count() != candidates.Count)
            {
                foreach (var action in candidates)
                {
                    var others = candidates.Where(x => x.Name == action.Name);
                    int counter = 0;
                    foreach (var other in others)
                        if (action != other)
                            other.Name = $"{other.Name}_{counter++}";
                }
            }
            return candidates;
        }

        internal abstract List<ActionDecl> GenerateCandidatesInner();

        internal ActionDecl GenerateMetaAction(string actionName, List<IExp> preconditions, List<IExp> effects)
        {
            var newAction = new ActionDecl(actionName);
            newAction.Parameters = new ParameterExp();
            newAction.Preconditions = new AndExp(newAction, preconditions);
            newAction.Effects = new AndExp(newAction, effects);

            // Stitch parameters together
            var all = newAction.FindTypes<PredicateExp>();
            foreach (var pred in all)
                foreach (var arg in pred.Arguments)
                    if (!newAction.Parameters.Values.Contains(arg))
                        newAction.Parameters.Values.Add(arg.Copy());

            return newAction;
        }
    }
}
