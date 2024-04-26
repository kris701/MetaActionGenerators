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
        public ArgsHandler Args { get; internal set; } = new ArgsHandler();
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
