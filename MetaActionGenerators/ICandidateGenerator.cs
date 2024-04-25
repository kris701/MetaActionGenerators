using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;

namespace MetaActionGenerators
{
    public interface ICandidateGenerator
    {
        public Dictionary<string, string> GeneratorArgs { get; }
        public DomainDecl Domain { get; }
        public List<ProblemDecl> Problems { get; }

        public List<ActionDecl> GenerateCandidates();
    }
}
