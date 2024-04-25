using PDDLSharp.Models.PDDL;

namespace MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction
{
    public class Candidate
    {
        public List<IExp> Preconditions { get; set; }
        public List<IExp> Effects { get; set; }

        public Candidate(List<IExp> preconditions, List<IExp> effects)
        {
            Preconditions = preconditions;
            Effects = effects;
        }

        public Candidate Copy()
        {
            var preconditions = new List<IExp>();
            foreach (var precon in Preconditions)
                preconditions.Add((IExp)precon.Copy());
            var effects = new List<IExp>();
            foreach (var effect in Effects)
                effects.Add((IExp)effect.Copy());

            return new Candidate(preconditions, effects);
        }
    }
}
