using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;

namespace MetaActionGenerators.Tests.CandidateGenerators
{
    [TestClass]
    public class CPDDLMutexMetaActionTests
    {
        [TestMethod]
        [DataRow("TestData/miconic/domain.pddl", 7, "TestData/miconic/p01.pddl", "TestData/miconic/p04.pddl", "TestData/miconic/p07.pddl")]
        [DataRow("TestData/blocksworld/domain.pddl", 19, "TestData/blocksworld/p01.pddl", "TestData/blocksworld/p04.pddl", "TestData/blocksworld/p07.pddl")]
        [DataRow("TestData/satellite/domain.pddl", 9, "TestData/satellite/p01.pddl", "TestData/satellite/p04.pddl", "TestData/satellite/p07.pddl")]
        public void Can_GenerateCorrectAmount(string domainFile, int expected, params string[] problemFiles)
        {
            // ARRANGE
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var domain = parser.ParseAs<DomainDecl>(new FileInfo(domainFile));
            var problems = new List<ProblemDecl>();
            foreach (var problemFile in problemFiles)
                problems.Add(parser.ParseAs<ProblemDecl>(new FileInfo(problemFile)));
            var generator = MetaGeneratorBuilder.GetGenerator(MetaGeneratorBuilder.GeneratorOptions.CPDDLMutexed, domain, problems, new Dictionary<string, string>()
            {
                { "cpddlExecutable", "../../../../Dependencies/cpddl/bin/pddl" },
                { "tempFolder", "tmp" }
            });

            // ACT
            var candidates = generator.GenerateCandidates();

            // ASSERT
            Assert.AreEqual(expected, candidates.Count);
        }
    }
}
