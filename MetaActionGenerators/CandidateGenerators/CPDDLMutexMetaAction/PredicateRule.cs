using PDDLSharp.Models.PDDL.Expressions;

namespace MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction
{
    public class PredicateRule
    {
        public string Predicate { get; set; }
        public List<string> Args { get; set; }
        public List<string> Types { get; set; }

        public PredicateRule(string predicate, List<string> args, List<string> types)
        {
            Predicate = predicate;
            Args = args;
            Types = types;
        }

        public bool IsEqual(PredicateExp pred)
        {
            if (pred.Name != Predicate) return false;
            if (pred.Arguments.Count != Types.Count) return false;
            for (int i = 0; i < Types.Count; i++)
                if (pred.Arguments[i].Type.Name != Types[i])
                    return false;
            return true;
        }

        public override string ToString()
        {
            var args = "";
            foreach (var arg in Args)
                args += $"{arg}, ";
            args = args.Trim();
            if (args.EndsWith(','))
                args = args.Substring(0, args.Length - 1);
            return $"{Predicate}: {args}";
        }
    }
}
