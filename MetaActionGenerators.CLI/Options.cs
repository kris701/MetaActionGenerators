using CommandLine;
using static MetaActionGenerators.MetaGeneratorBuilder;

namespace MetaActionGenerators.CLI
{
    public class Options
    {
        [Option("domain", Required = true, HelpText = "Path to the domain file")]
        public string DomainPath { get; set; } = "";
        [Option("problems", Required = true, HelpText = "Path to the problem files")]
        public IEnumerable<string> ProblemsPath { get; set; } = new List<string>();
        [Option("generator", Required = true, HelpText = "What generator to use.")]
        public GeneratorOptions GeneratorOption { get; set; }

        [Option("out", Required = false, HelpText = "Path to where the candidates should be outputted to.")]
        public string OutPath { get; set; } = "";
        [Option("args", Required = false, HelpText = "Optional arguments for the generator. Some generators require specific arguments, others do not. The arguments are in key-pairs, in the format key;value")]
        public IEnumerable<string> Args { get; set; } = new List<string>();
    }
}
