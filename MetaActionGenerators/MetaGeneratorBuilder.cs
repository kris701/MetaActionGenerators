using MetaActionGenerators.CandidateGenerators;
using MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction;
using MetaActionGenerators.CandidateGenerators.CSMMacrosMetaAction;
using PDDLSharp.Models.PDDL;

namespace MetaActionGenerators
{
    public static class MetaGeneratorBuilder
    {
        public enum GeneratorOptions { CPDDLMutexed, Flipped, Predicate, Stripped, PDDLSharpMacrosReductionMetaAction }
        private static Dictionary<GeneratorOptions, Func<PDDLDecl, Dictionary<string, string>, ICandidateGenerator>> _dict = new Dictionary<GeneratorOptions, Func<PDDLDecl, Dictionary<string, string>, ICandidateGenerator>>()
        {
            { GeneratorOptions.CPDDLMutexed, (d, a) => new CPDDLMutexedMetaActions(a, d) },
            { GeneratorOptions.Flipped, (d, a) => new FlipMetaActions(d) },
            { GeneratorOptions.Predicate, (d, a) => new PredicateMetaActions(d) },
            { GeneratorOptions.Stripped, (d, a) => new StrippedMetaActions(d) },
            { GeneratorOptions.PDDLSharpMacrosReductionMetaAction, (d, a) => new PDDLSharpMutexedMetaActions(a, d) },
        };

        public static ICandidateGenerator GetGenerator(GeneratorOptions opt, PDDLDecl decl, Dictionary<string, string> args) => _dict[opt](decl, args);
    }
}
