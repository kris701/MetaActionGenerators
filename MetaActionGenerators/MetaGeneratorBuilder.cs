using MetaActionGenerators.CandidateGenerators;
using MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace MetaActionGenerators
{
    public static class MetaGeneratorBuilder
    {
        public enum GeneratorOptions { CPDDLMutexed, Flipped, Predicate, Stripped, PDDLSharpMacrosReduction, PreconditionPermutationReduction }
        private static readonly Dictionary<GeneratorOptions, Func<DomainDecl, List<ProblemDecl>, Dictionary<string, string>, ICandidateGenerator>> _dict = new Dictionary<GeneratorOptions, Func<DomainDecl, List<ProblemDecl>, Dictionary<string, string>, ICandidateGenerator>>()
        {
            { GeneratorOptions.CPDDLMutexed, (d, p, a) => new CPDDLMutexedMetaActions(a, d, p) },
            { GeneratorOptions.Flipped, (d, p, a) => new FlipMetaActions(d, p) },
            { GeneratorOptions.Predicate, (d, p, a) => new PredicateMetaActions(d, p) },
            { GeneratorOptions.Stripped, (d, p, a) => new StrippedMetaActions(d, p) },
            { GeneratorOptions.PDDLSharpMacrosReduction, (d, p, a) => new PDDLSharpMacroReductionMetaActions(a, d, p) },
            { GeneratorOptions.PreconditionPermutationReduction, (d, p, a) => new PreconditionPermutationReductionMetaActions(d, p) },
        };

        public static ICandidateGenerator GetGenerator(GeneratorOptions opt, DomainDecl domain, List<ProblemDecl> problems, Dictionary<string, string> args) => _dict[opt](domain, problems, args);
    }
}
