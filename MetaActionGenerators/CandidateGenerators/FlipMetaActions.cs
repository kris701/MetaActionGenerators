using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;

namespace MetaActionGenerators.CandidateGenerators
{
    /// <summary>
    /// Assumed every predicate can be a flipped, and constructs meta actions out of them
    /// </summary>
    public class FlipMetaActions : BaseCandidateGenerator
    {
        public FlipMetaActions(DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            if (Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");

            var candidates = new List<ActionDecl>();
            foreach (var predicate in Domain.Predicates.Predicates)
            {
                if (!Statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
                {
                    candidates.Add(GenerateMetaAction(
                        $"meta_{predicate.Name}_0",
                        new List<IExp>() { new NotExp(predicate) },
                        new List<IExp>() { predicate }));
                    candidates.Add(GenerateMetaAction(
                        $"meta_{predicate.Name}_1",
                        new List<IExp>() { predicate },
                        new List<IExp>() { new NotExp(predicate) }));
                }
            }

            return candidates.Distinct(Domain.Actions);
        }
    }
}
