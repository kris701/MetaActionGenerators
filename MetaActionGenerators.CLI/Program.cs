﻿using CommandLine;
using CommandLine.Text;
using MetaActionGenerators.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.PDDL;
using System;

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
            PathHelper.RecratePath(opts.OutPath);
            Console.WriteLine("Parsing problem and domain files...");
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var domain = parser.ParseAs<DomainDecl>(new FileInfo(opts.DomainPath));
            var problems = new List<ProblemDecl>();
            foreach(var problemFile in opts.ProblemsPath)
                problems.Add(parser.ParseAs<ProblemDecl>(new FileInfo(problemFile)));

            Console.WriteLine("Parsing args...");
            var args = new Dictionary<string, string>();
            foreach(var keyvalue in opts.Args)
            {
                var key = keyvalue.Substring(0, keyvalue.IndexOf(';')).Trim();
                var value = keyvalue.Substring(keyvalue.IndexOf(';') + 1).Trim();
                args.Add(key, value);
            }

            Console.WriteLine("Initialising Generator...");
            var generator = MetaGeneratorBuilder.GetGenerator(opts.GeneratorOption, domain, problems, args);

            Console.WriteLine("Generating Candidates...");
            var candidates = generator.GenerateCandidates();
            Console.WriteLine($"A total of {candidates.Count} candidates generated.");

            Console.WriteLine("Outputting Candidates...");
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