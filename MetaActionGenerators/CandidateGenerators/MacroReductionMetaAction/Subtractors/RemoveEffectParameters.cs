using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Overloads;

namespace MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction.Subtractors
{
    public class RemoveEffectParameters : BaseMetaGenerator
    {
        /// <summary>
        /// "C_{eff}, eliminates a parameter appearing in the effects, removing any precondition and/or effect that depends on it."
        /// </summary>
        /// <param name="macro"></param>
        /// <returns></returns>
        public override List<ActionDecl> Generate(ActionDecl macro)
        {
            List<ActionDecl> metaActions = new List<ActionDecl>();

            macro.EnsureAnd();
            foreach (var arg in macro.Parameters.Values)
            {
                if (macro.Effects.FindNames(arg.Name).Count > 0)
                {
                    var newMetaAction = macro.Copy();

                    newMetaAction.Parameters.Values.RemoveAll(x => x.Name == arg.Name);
                    RemoveName(newMetaAction.Preconditions, arg.Name);
                    RemoveName(newMetaAction.Effects, arg.Name);

                    RemoveUnusedParameters(newMetaAction);
                    metaActions.Add(newMetaAction);
                }
            }

            return metaActions;
        }
    }
}
