using MetaActionGenerators.CandidateGenerators;
using MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction;
using MetaActionGenerators.CandidateGenerators.CSMMacrosMetaAction;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace MetaActionGenerators
{
    public static class MetaGeneratorBuilder
    {
        public enum GeneratorOptions { CPDDLMutexed, Flipped, Predicate, Stripped, PDDLSharpMacrosReductionMetaAction }
        private static Dictionary<GeneratorOptions, Func<DomainDecl, List<ProblemDecl>, Dictionary<string, string>, ICandidateGenerator>> _dict = new Dictionary<GeneratorOptions, Func<DomainDecl, List<ProblemDecl>, Dictionary<string, string>, ICandidateGenerator>>()
        {
            { GeneratorOptions.CPDDLMutexed, (d, p, a) => new CPDDLMutexedMetaActions(a, d, p) },
            { GeneratorOptions.Flipped, (d, p, a) => new FlipMetaActions(d, p) },
            { GeneratorOptions.Predicate, (d, p, a) => new PredicateMetaActions(d, p) },
            { GeneratorOptions.Stripped, (d, p, a) => new StrippedMetaActions(d, p) },
            { GeneratorOptions.PDDLSharpMacrosReductionMetaAction, (d, p, a) => new PDDLSharpMutexedMetaActions(a, d, p) },
        };

        public static ICandidateGenerator GetGenerator(GeneratorOptions opt, DomainDecl domain, List<ProblemDecl> problems, Dictionary<string, string> args) => _dict[opt](domain, problems, args);
    }
}
