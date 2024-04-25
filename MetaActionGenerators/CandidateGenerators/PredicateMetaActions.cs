﻿using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;

namespace MetaActionGenerators.CandidateGenerators
{
    /// <summary>
    /// Takes all non-static predicates and makes meta actions based on no preconditions and simply the predicate.
    /// Both a normal and a negated version is made for each predicate
    /// </summary>
    public class PredicateMetaActions : BaseCandidateGenerator
    {
        public PredicateMetaActions(PDDLDecl decl) : base(decl)
        {
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            if (Decl.Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");

            var candidates = new List<ActionDecl>();
            foreach (var predicate in Decl.Domain.Predicates.Predicates)
            {
                if (!Statics.Any(x => x.Name.ToUpper() == predicate.Name.ToUpper()))
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

            return candidates.Distinct(Decl.Domain.Actions);
        }
    }
}
