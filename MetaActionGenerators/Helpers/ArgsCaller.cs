using System.Diagnostics;
using System.Text;

namespace MetaActionGenerators.Helpers
{
    public class ArgsCaller : IDisposable
    {
        public event DataReceivedEventHandler? StdErr;
        public event DataReceivedEventHandler? StdOut;

        public string Program { get; }
        public Dictionary<string, string> Arguments { get; }
        public Dictionary<string, string> Environment { get; }
        public Process Process { get; internal set; }

        public ArgsCaller(string program, Dictionary<string, string> arguments)
        {
            Program = program;
            Arguments = arguments;
            Environment = new Dictionary<string, string>();

            Process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Program,
                    Arguments = "",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            };
        }

        public ArgsCaller(string program)
        {
            Program = program;
            Arguments = new Dictionary<string, string>();
            Environment = new Dictionary<string, string>();

            Process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Program,
                    Arguments = "",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            };
        }

        public int Run()
        {
            foreach (var key in Arguments.Keys)
                if (Path.IsPathFullyQualified(Arguments[key]))
                    Arguments[key] = PathHelper.RootPath(Arguments[key]);
            StringBuilder sb = new StringBuilder("");
            foreach (var key in Arguments.Keys)
                sb.Append($"{key} {Arguments[key]} ");

            Process.StartInfo.Arguments = sb.ToString();
            foreach (var key in Environment.Keys)
                Process.StartInfo.Environment.Add(key, Environment[key]);
            if (StdErr != null)
            {
                Process.StartInfo.RedirectStandardError = true;
                Process.ErrorDataReceived += StdErr;
            }
            if (StdOut != null)
            {
                Process.StartInfo.RedirectStandardOutput = true;
                Process.OutputDataReceived += StdOut;
            }

            Process.Start();
            if (StdErr != null)
                Process.BeginErrorReadLine();

            if (StdOut != null)
                Process.BeginOutputReadLine();

            Process.WaitForExit();
            return Process.ExitCode;
        }

        public void Dispose()
        {
            if (Process != null && !Process.HasExited)
                Process.Kill();
        }
    }
}
