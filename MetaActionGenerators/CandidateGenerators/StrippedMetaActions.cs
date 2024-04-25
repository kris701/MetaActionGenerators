using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;

namespace MetaActionGenerators.CandidateGenerators
{
    /// <summary>
    /// Generates meta actions by taking normal actions and removing all (non-static) preconditions from it.
    /// </summary>
    public class StrippedMetaActions : BaseCandidateGenerator
    {
        public StrippedMetaActions(DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            var candidates = new List<ActionDecl>();
            foreach (var action in Domain.Actions)
            {
                action.EnsureAnd();
                if (action.Effects is AndExp and)
                    candidates.Add(GenerateMetaAction(
                        $"meta_{action.Name}",
                        new List<IExp>(),
                        and.Children));
            }

            return candidates.Distinct(Domain.Actions);
        }
    }
}
