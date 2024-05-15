using PDDLSharp.Models.PDDL;

namespace MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction
{
    public class Candidate
    {
        public List<IExp> Preconditions { get; set; }
        public Dictionary<IExp, List<int>> Effects { get; set; }

        public Candidate(List<IExp> preconditions, Dictionary<IExp, List<int>> upholdingEffects)
        {
            Preconditions = preconditions;
            Effects = upholdingEffects;
        }

        public Candidate Copy()
        {
            var preconditions = new List<IExp>();
            foreach (var precon in Preconditions)
                preconditions.Add((IExp)precon.Copy());
            var upholding = new Dictionary<IExp, List<int>>();
            foreach (var key in Effects.Keys)
                upholding.Add(key, new List<int>(Effects[key]));

            return new Candidate(preconditions, upholding);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Candidate other)
            {
                if (other.Preconditions.Count != Preconditions.Count) return false;
                if (other.Effects.Keys.Count != Effects.Keys.Count) return false;
                foreach (var precon in other.Preconditions)
                    if (!Preconditions.Contains(precon))
                        return false;
                foreach (var precon in Preconditions)
                    if (!other.Preconditions.Contains(precon))
                        return false;
                foreach (var effect in other.Effects.Keys)
                    if (!Effects.ContainsKey(effect))
                        return false;
                foreach (var effect in Effects.Keys)
                    if (!other.Effects.ContainsKey(effect))
                        return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var code = 10;

            foreach (var precon in Preconditions)
                code ^= precon.GetHashCode();
            foreach (var effect in Effects.Keys)
                code ^= effect.GetHashCode();

            return code;
        }
    }
}
