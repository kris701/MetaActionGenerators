using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Overloads;

namespace MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction.Subtractors
{
    public class RemovePreconditionParameters : BaseMetaGenerator
    {
        /// <summary>
        /// "C_{pre} removes parameters that are absent in the effect of the macro"
        /// </summary>
        /// <param name="macro"></param>
        /// <returns></returns>
        public override List<ActionDecl> Generate(ActionDecl macro)
        {
            var metaActions = new List<ActionDecl>();

            macro.EnsureAnd();
            List<int> removeable = new List<int>();
            for (int i = 0; i < macro.Parameters.Values.Count; i++)
                if (macro.Effects.FindNames(macro.Parameters.Values[i].Name).Count == 0)
                    removeable.Add(i);

            if (removeable.Count == 0)
                return new List<ActionDecl>();

            var permutations = GeneratePermutations(removeable.Count);
            foreach (var premutation in permutations)
            {
                var toRemove = new List<string>();
                for (int i = 0; i < premutation.Length; i++)
                    if (!premutation[i])
                        toRemove.Add(macro.Parameters.Values[removeable[i]].Name);
                if (toRemove.Count == 0)
                    continue;

                var newMetaAction = macro.Copy();
                foreach (var remove in toRemove)
                {
                    newMetaAction.Parameters.Values.RemoveAll(x => x.Name == remove);
                    RemoveName(newMetaAction.Preconditions, remove);
                }

                metaActions.Add(newMetaAction);
            }

            return metaActions;
        }

        private Queue<bool[]> GeneratePermutations(int count)
        {
            var returnQueue = new Queue<bool[]>();
            GeneratePermutations(count, new bool[count], 0, returnQueue);
            return returnQueue;
        }

        private void GeneratePermutations(int count, bool[] source, int index, Queue<bool[]> returnQueue)
        {
            var trueSource = new bool[count];
            Array.Copy(source, trueSource, count);
            trueSource[index] = true;
            if (index < count - 1)
                GeneratePermutations(count, trueSource, index + 1, returnQueue);
            else
                returnQueue.Enqueue(trueSource);

            var falseSource = new bool[count];
            Array.Copy(source, falseSource, count);
            falseSource[index] = false;
            if (index < count - 1)
                GeneratePermutations(count, falseSource, index + 1, returnQueue);
            else
                returnQueue.Enqueue(falseSource);
        }
    }
}
