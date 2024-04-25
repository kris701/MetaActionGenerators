using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Tools;
using PDDLSharp.Translators.Tools;

namespace MetaActionGenerators.CandidateGenerators
{
    public abstract class BaseCandidateGenerator : ICandidateGenerator
    {
        public virtual Dictionary<string, string> GeneratorArgs { get; internal set; } = new Dictionary<string, string>();
        public PDDLDecl Decl { get; }

        public List<PredicateExp> Statics = new List<PredicateExp>();
        public List<PredicateExp> SimpleStatics = new List<PredicateExp>();

        protected BaseCandidateGenerator(PDDLDecl decl)
        {
            Decl = decl;
            ContextualizeIfNotAlready(Decl);
            Statics = SimpleStaticPredicateDetector.FindStaticPredicates(decl);
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
            return candidates;
        }

        internal abstract List<ActionDecl> GenerateCandidatesInner();

        internal void ContextualizeIfNotAlready(PDDLDecl pddlDecl)
        {
            if (!pddlDecl.IsContextualised)
            {
                var listener = new ErrorListener();
                var contextualiser = new PDDLContextualiser(listener);
                contextualiser.Contexturalise(pddlDecl);
            }
        }

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

        internal PredicateExp GetEqualsPredicate(PredicateExp pred1, PredicateExp pred2)
        {
            var args = new List<NameExp>();
            for (int i = 0; i < pred1.Arguments.Count; i++)
            {
                if (pred1.Arguments[i].Name != pred2.Arguments[i].Name)
                {
                    args.Add(pred1.Arguments[i]);
                    args.Add(pred2.Arguments[i]);
                }
            }

            return new PredicateExp("=", args);
        }
    }
}
