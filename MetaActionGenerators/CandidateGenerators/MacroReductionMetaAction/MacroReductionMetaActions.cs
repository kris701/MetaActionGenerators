﻿using MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction.Subtractors;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Problem;

namespace MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction
{
    public class MacroReductionMetaActions : BaseCandidateGenerator
    {
        public ActionDecl Macro { get; set; }

        public MacroReductionMetaActions(DomainDecl domain, List<ProblemDecl> problems, ActionDecl macro) : base(domain, problems)
        {
            Macro = macro;
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            var candidates = new List<ActionDecl>();

            var preconRemover = new RemovePreconditionParameters();
            candidates.AddRange(preconRemover.Generate(Macro));
            var effRemover = new RemoveEffectParameters();
            candidates.AddRange(effRemover.Generate(Macro));
            var additionalEffRemover = new RemoveEffectParameters();
            candidates.AddRange(additionalEffRemover.Generate(Macro));

            candidates.RemoveAll(x => x.Effects is AndExp and && and.Children.Count == 0);

            return candidates;
        }
    }
}
