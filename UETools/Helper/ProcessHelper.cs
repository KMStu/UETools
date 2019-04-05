using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UETools.Helper
{
    public static class ProcessHelper
    {
        public static int RunProcess(string filename, string arguments, string workingDirectory, out string output, int timeoutMS, CancellationToken ct)
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = filename;
                process.StartInfo.Arguments = arguments;
                if (workingDirectory != null)
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                }

                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) => { if (e.Data == null) { outputWaitHandle.Set(); } else { outputBuilder.AppendLine(e.Data); } };
                    process.ErrorDataReceived += (sender, e) => { if (e.Data == null) { errorWaitHandle.Set(); } else { errorBuilder.AppendLine(e.Data); } };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    DateTime timeoutTime = DateTime.Now + TimeSpan.FromMilliseconds(timeoutMS > 0 ? timeoutMS : 30000);
                    do
                    {
                        if ( process.WaitForExit(100) && outputWaitHandle.WaitOne(100) && errorWaitHandle.WaitOne(100) )
                        {
                            output = (process.ExitCode == 0) ? outputBuilder.ToString() : errorBuilder.ToString();
                            return process.ExitCode;
                        }

                        if (ct.IsCancellationRequested)
                        {
                            process.Kill();
                            ct.ThrowIfCancellationRequested();
                            break;
                        }
                    } while (DateTime.Now < timeoutTime);
                }

                throw new ApplicationException(string.Format("Failed to run process '{0}' before we timed out", filename));
            }
        }

        public static int RunProcess(string filename, string arguments, string workingDirectory, out string output)
        {
            return RunProcess(filename, arguments, workingDirectory, out output, 0, new CancellationToken());
        }

        public static int RunProcess(string filename, string arguments, string workingDirectory)
        {
            string dummy;
            return RunProcess(filename, arguments, workingDirectory, out dummy, 0, new CancellationToken());
        }

        public static int RunProcess(string filename, string arguments, out string output, int timeoutMS, CancellationToken ct)
        {
            return RunProcess(filename, arguments, null, out output, 0, new CancellationToken());
        }

        public static int RunProcess(string filename, string arguments, out string output)
        {
            return RunProcess(filename, arguments, out output, 0, new CancellationToken());
        }

        public static int RunProcess(string filename, string arguments)
        {
            string dummy;
            return RunProcess(filename, arguments, out dummy, 0, new CancellationToken());
        }
    }
}
