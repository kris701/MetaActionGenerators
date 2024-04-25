using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction
{
    public class PredicateRule
    {
        public string Predicate { get; set; }
        public List<string> Args { get; set; }

        public PredicateRule(string predicate, List<string> args)
        {
            Predicate = predicate;
            Args = args;
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
