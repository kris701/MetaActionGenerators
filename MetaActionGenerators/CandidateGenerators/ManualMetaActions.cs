using MetaActionGenerators.ArgumentSystem;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;

namespace MetaActionGenerators.CandidateGenerators
{
    /// <summary>
    /// A simple "generator" that simply outputs some manually given meta actions from a path.
    /// </summary>
    public class ManualMetaActions : BaseCandidateGenerator
    {
        public ManualMetaActions(Dictionary<string, string> generatorArgs, DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
            Args = new ArgsHandler(new List<Arg>()
            {
                new Arg("metaPath", "A path to the folder containing the meta actions.")
            }, generatorArgs);
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            if (!Directory.Exists(Args.GetArgument<string>("metaPath")))
                throw new DirectoryNotFoundException($"Meta action directory not found: {Args.GetArgument<string>("metaPath")}");
            var candidates = new List<ActionDecl>();

            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            foreach (var file in Directory.GetFiles(Args.GetArgument<string>("metaPath")))
                candidates.Add(parser.ParseAs<ActionDecl>(new FileInfo(file)));

            return candidates;
        }
    }
}
