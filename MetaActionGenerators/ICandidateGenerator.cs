using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;

namespace MetaActionGenerators
{
    public interface ICandidateGenerator
    {
        public Dictionary<string, string> GeneratorArgs { get; }
        public PDDLDecl Decl { get; }

        public List<ActionDecl> GenerateCandidates();
    }
}
