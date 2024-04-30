using MetaActionGenerators.ArgumentSystem;
using MetaActionGenerators.CandidateGenerators.MacroReductionMetaAction;
using MetaActionGenerators.Helpers;
using PDDLSharp.CodeGenerators.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Models.PDDL.Overloads;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Parsers.FastDownward.Plans;
using PDDLSharp.Parsers.PDDL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MetaActionGenerators.CandidateGenerators.CSMMacroReductionMetaAction
{
    public class CSMMacroReductionMetaActions : BaseCandidateGenerator
    {
        public CSMMacroReductionMetaActions(Dictionary<string, string> generatorArgs, DomainDecl domain, List<ProblemDecl> problems) : base(domain, problems)
        {
            Args = new ArgsHandler(new List<Arg>()
            {
                new Arg("csmPath", "The path to the CSMs folder. It should be the root folder of CSMs!"),
                new Arg("tempFolder", "A folder to store temporary files."),
                new Arg("fastDownwardPath", "A path to a build of Fast Downward. This should be to the `fast-downward.py` file."),
                new Arg("log", false, "Output stdout from CSM")
            }, generatorArgs);
        }

        internal override List<ActionDecl> GenerateCandidatesInner()
        {
            var csmPath = PathHelper.RootPath(Args.GetArgument<string>("csmPath"));
            var tempPath = PathHelper.RootPath(Args.GetArgument<string>("tempFolder"));

            SetupTempFilesAndScripts(tempPath, csmPath);

            var macros = RunMUM(tempPath);
            macros.AddRange(RunBLOOMA(tempPath));

            var candidates = new List<ActionDecl>();
            foreach (var macro in macros)
            {
                var reducer = new MacroReductionMetaActions(Domain, Problems, macro);
                candidates.AddRange(reducer.GenerateCandidates());
            }

            return candidates.Distinct(Domain.Actions);
        }

        private List<ActionDecl> RunMUM(string tempPath)
        {
            var doLog = Args.GetArgument<bool>("log");
            using (ArgsCaller csmCaller = new ArgsCaller(Path.Combine(tempPath, "scripts", "learn-mum.sh")))
            {
                csmCaller.StdOut += (s, o) =>
                {
                    if (doLog)
                        Console.WriteLine(o.Data);
                };
                csmCaller.StdErr += (s, o) =>
                {
                    if (doLog)
                        Console.WriteLine(o.Data);
                };
                csmCaller.Arguments.Add("../target", "");
                csmCaller.Arguments.Add("lama-script-fixed", "");
                csmCaller.Process.StartInfo.WorkingDirectory = Path.Combine(tempPath, "scripts");
                if (csmCaller.Run() != 0)
                    throw new Exception("CSM failed!");
            }

            if (!File.Exists(Path.Combine(tempPath, "target", "domain_mum.pddl")))
                return new List<ActionDecl>();

            return GetNewActions(Path.Combine(tempPath, "target", "domain_mum.pddl"));
        }

        private List<ActionDecl> RunBLOOMA(string tempPath)
        {
            var doLog = Args.GetArgument<bool>("log");
            using (ArgsCaller csmCaller = new ArgsCaller(Path.Combine(tempPath, "scripts", "learn-bloma.sh")))
            {
                csmCaller.StdOut += (s, o) =>
                {
                    if (doLog)
                        Console.WriteLine(o.Data);
                };
                csmCaller.StdErr += (s, o) =>
                {
                    if (doLog)
                        Console.WriteLine(o.Data);
                };
                csmCaller.Arguments.Add("../target", "");
                csmCaller.Arguments.Add("lama-script-fixed", "");
                csmCaller.Process.StartInfo.WorkingDirectory = Path.Combine(tempPath, "scripts");
                if (csmCaller.Run() != 0)
                    throw new Exception("CSM failed!");
            }

            if (!File.Exists(Path.Combine(tempPath, "target", "domain_bloma.pddl")))
                return new List<ActionDecl>();

            return GetNewActions(Path.Combine(tempPath, "target", "domain_bloma.pddl"));
        }

        private List<ActionDecl> GetNewActions(string domain)
        {
            var listener = new ErrorListener();
            var parser = new PDDLParser(listener);
            var enhanced = parser.ParseAs<DomainDecl>(new FileInfo(domain));
            var candidates = enhanced.Actions.Where(x => !Domain.Actions.Any(y => x.Name == y.Name)).ToList();
            foreach(var candidate in candidates)
            {
                if (candidate.Preconditions is AndExp and)
                {
                    and.Children.RemoveAll(x => x is PredicateExp pred && pred.Name.ToUpper().StartsWith("STAI"));
                }
            }
            return candidates;
        }

        private void SetupTempFilesAndScripts(string tempPath, string csmPath)
        {
            PathHelper.RecratePath(tempPath);
            PathHelper.RecratePath(Path.Combine(tempPath, "scripts"));
            PathHelper.RecratePath(Path.Combine(tempPath, "src"));
            CopyFilesRecursively(Path.Combine(csmPath, "scripts"), Path.Combine(tempPath, "scripts"));
            CopyFilesRecursively(Path.Combine(csmPath, "src"), Path.Combine(tempPath, "src"));

            var text = GetLamaScript();
            text = text.Replace("{{FDPATH}}", PathHelper.RootPath(Args.GetArgument<string>("fastDownwardPath")));
            File.WriteAllText(Path.Combine(tempPath, "scripts", "lama-script-fixed"), text);

            Directory.CreateDirectory(Path.Combine(tempPath, "target", "learn"));
            Directory.CreateDirectory(Path.Combine(tempPath, "target", "testing"));
            var listener = new ErrorListener();
            var codeGenerator = new PDDLCodeGenerator(listener);
            codeGenerator.Generate(Domain, Path.Combine(tempPath, "target", "domain.pddl"));
            int counter = 1;
            foreach(var problem in Problems)
                codeGenerator.Generate(problem, Path.Combine(tempPath, "target", "learn", $"p{counter++}.pddl"));
        }

        public string GetLamaScript()
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith("lama-script"));

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}
