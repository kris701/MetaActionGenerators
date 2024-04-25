using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;

namespace MetaActionGenerators.CandidateGenerators
{
    public class PreconditionPermutationReductionMetaActions : BaseCandidateGenerator
    {
        public PreconditionPermutationReductionMetaActions(DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            if (Domain.Predicates == null)
                throw new Exception("No predicates defined in domain!");

            var candidates = new List<ActionDecl>();
            int metaCounter = 0;
            foreach (var action in Domain.Actions)
            {
                if (action.Preconditions is AndExp and)
                {
                    var permutations = GeneratePermutations(and, Statics);
                    foreach (var permutation in permutations)
                    {
                        var copy = action.Copy();
                        copy.Name = $"$meta_{metaCounter++}";
                        var copyPrecon = new AndExp();
                        copy.Preconditions = copyPrecon;
                        for (int i = 0; i < permutation.Length; i++)
                            if (permutation[i] && and.Children[i].Copy() is IExp child)
                                copyPrecon.Children.Add(child);

                        var notReferenced = new List<NameExp>();
                        foreach (var param in copy.Parameters.Values)
                            if (!IsReferenced(copy, param, Statics))
                                notReferenced.Add(param);

                        var notReferenced2 = new List<NameExp>();
                        foreach (var param in notReferenced)
                        {
                            var allRefs = copy.FindNames(param.Name);
                            bool isReferences = false;
                            foreach (var refs in allRefs)
                            {
                                if (refs.Parent is PredicateExp pred && pred.Arguments.Any(x => IsReferenced(copy, x, notReferenced)))
                                {
                                    isReferences = true;
                                    break;
                                }
                            }
                            if (!isReferences)
                                notReferenced2.Add(param);
                        }

                        var toRemove = new List<INode>();
                        foreach (var notRefed in notReferenced2)
                        {
                            var allRefs = copy.FindNames(notRefed.Name);
                            toRemove.Add(notRefed);
                            foreach (var refe in allRefs)
                                if (refe.Parent is PredicateExp pred)
                                    toRemove.Add(pred);
                        }

                        foreach (var remove in toRemove)
                        {
                            var useful = GetMostUsefullParent(remove);
                            if (useful.Parent is IListable list)
                                list.Remove(useful);
                        }

                        if (!copy.Preconditions.Equals(action.Preconditions))
                            candidates.Add(copy);
                    }
                }
            }

            return candidates.Distinct(Domain.Actions);
        }

        private bool IsReferenced(INode copy, NameExp param, List<PredicateExp> statics)
        {
            var allRefs = copy.FindNames(param.Name);
            bool isReferenced = false;
            foreach (var refs in allRefs)
            {
                if (refs.Parent is PredicateExp pred && !statics.Any(x => x.Name == pred.Name))
                {
                    isReferenced = true;
                    break;
                }
            }
            return isReferenced;
        }

        private bool IsReferenced(INode copy, NameExp param, List<NameExp> notReferenced)
        {
            var allRefs = copy.FindNames(param.Name);
            bool isReferenced = false;
            foreach (var refs in allRefs)
            {
                if (refs.Parent is PredicateExp pred && !notReferenced.Any(x => x.Name == pred.Name))
                {
                    isReferenced = true;
                    break;
                }
            }
            return isReferenced;
        }

        private INode GetMostUsefullParent(INode from)
        {
            if (from.Parent is IListable)
                return from;
            if (from.Parent == null)
                throw new ArgumentNullException("Expected a parent");
            return GetMostUsefullParent(from.Parent);
        }

        private Queue<bool[]> GeneratePermutations(AndExp preconditions, List<PredicateExp> statics)
        {
            var queue = new Queue<bool[]>();
            GeneratePermutations(preconditions, new bool[preconditions.Children.Count], 0, statics, queue);
            return queue;
        }

        private void GeneratePermutations(AndExp preconditions, bool[] source, int index, List<PredicateExp> statics, Queue<bool[]> returnQueue)
        {
            var trueSource = new bool[source.Length];
            Array.Copy(source, trueSource, source.Length);
            trueSource[index] = true;
            if (index < source.Length - 1)
                GeneratePermutations(preconditions, trueSource, index + 1, statics, returnQueue);
            else
                returnQueue.Enqueue(trueSource);

            if (!statics.Any(x => preconditions.Children[index] is PredicateExp pred && pred.Name == x.Name))
            {
                var falseSource = new bool[source.Length];
                Array.Copy(source, falseSource, source.Length);
                falseSource[index] = false;
                if (index < source.Length - 1)
                    GeneratePermutations(preconditions, falseSource, index + 1, statics, returnQueue);
                else
                    returnQueue.Enqueue(falseSource);
            }
        }
    }
}
