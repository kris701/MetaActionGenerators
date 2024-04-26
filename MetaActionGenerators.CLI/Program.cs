using CommandLine;
using CommandLine.Text;
using MetaActionGenerators.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;

namespace MetaActionGenerators.CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithNotParsed(errs => DisplayHelp(parserResult, errs));
            parserResult.WithParsed(Run);
        }

        public static void Run(Options opts)
        {
            opts.DomainPath = PathHelper.RootPath(opts.DomainPath);
            var problemFiles = new List<string>(opts.ProblemsPath);
            for (int i = 0; i < problemFiles.Count; i++)
                problemFiles[i] = PathHelper.RootPath(problemFiles[i]);
            opts.ProblemsPath = problemFiles;
            opts.OutPath = PathHelper.RootPath(opts.OutPath);

            Console.WriteLine("Tool started with following arguments:");
            Console.WriteLine("======================================");
            Console.WriteLine($"Domain: {opts.DomainPath}");
            Console.WriteLine($"Problems: {string.Join(',', opts.ProblemsPath)}");
            Console.WriteLine($"Output Path: {opts.OutPath}");
            Console.WriteLine($"Generator: {Enum.GetName(opts.GeneratorOption)}");
            Console.WriteLine($"Additional Arguments: {string.Join(',', opts.Args)}");
            Console.WriteLine("======================================");

            Console.WriteLine("Parsing problem and domain files...");
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            Console.WriteLine($"\tParsing Domain");
            var domain = parser.ParseAs<DomainDecl>(new FileInfo(opts.DomainPath));
            Console.WriteLine($"\tParsing Problems");
            Console.WriteLine($"\tA total of {opts.ProblemsPath.Count()} problems to parse.");
            var problems = new List<ProblemDecl>();
            foreach (var problemFile in opts.ProblemsPath)
                problems.Add(parser.ParseAs<ProblemDecl>(new FileInfo(problemFile)));

            Console.WriteLine("Parsing args...");
            Console.WriteLine($"\tA total of {opts.Args.Count()} additional arguments to parse.");
            var args = new Dictionary<string, string>();
            foreach (var keyvalue in opts.Args)
            {
                var key = keyvalue.Substring(0, keyvalue.IndexOf(';')).Trim();
                var value = keyvalue.Substring(keyvalue.IndexOf(';') + 1).Trim();
                args.Add(key, value);
            }

            Console.WriteLine($"Initialising Generator '{Enum.GetName(opts.GeneratorOption)}'...");
            var generator = MetaGeneratorBuilder.GetGenerator(opts.GeneratorOption, domain, problems, args);

            Console.WriteLine("Generating Candidates...");
            var candidates = generator.GenerateCandidates();
            Console.WriteLine($"\tA total of {candidates.Count} candidates generated.");

            Console.WriteLine("Outputting Candidates...");
            Console.WriteLine($"\tCandidates are outputted to '{opts.OutPath}'");
            PathHelper.RecratePath(opts.OutPath);
            var codeGenerator = new PDDLCodeGenerator(listener);
            foreach (var candidate in candidates)
                codeGenerator.Generate(candidate, Path.Combine(opts.OutPath, $"{candidate.Name}.pddl"));
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            var sentenceBuilder = SentenceBuilder.Create();
            foreach (var error in errs)
                if (error is not HelpRequestedError)
                    Console.WriteLine(sentenceBuilder.FormatError(error));
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddEnumValuesToHelpText = true;
                return h;
            }, e => e, verbsIndex: true);
            Console.WriteLine(helpText);
            HandleParseError(errs);
        }
    }
}
