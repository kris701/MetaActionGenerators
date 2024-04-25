using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;

namespace MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction.Subtractors
{
    public abstract class BaseMetaGenerator
    {
        public abstract List<ActionDecl> Generate(ActionDecl macro);

        internal void RemoveName(INode node, string name)
        {
            var allRefs = node.FindNames(name);
            foreach (var sub in allRefs)
            {
                var usefullParent = GetMostUsefullParent(sub);
                if (usefullParent.Parent is IListable list)
                    list.Remove(usefullParent);
            }
        }

        private INode GetMostUsefullParent(INode from)
        {
            if (from.Parent is AndExp || from.Parent is OrExp)
                return from;
            if (from.Parent == null)
                throw new ArgumentNullException("Expected a parent");
            return GetMostUsefullParent(from.Parent);
        }

        internal void RemoveUnusedParameters(ActionDecl action)
        {
            List<NameExp> newParams = new List<NameExp>();
            foreach (var arg in action.Parameters.Values)
                if (action.FindNames(arg.Name).Count > 1)
                    newParams.Add(arg);
            action.Parameters.Values = newParams;
        }
    }
}