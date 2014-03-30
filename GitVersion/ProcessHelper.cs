namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

    static class ProcessHelper
    {
        private static volatile object _lockObject = new object();

        // http://social.msdn.microsoft.com/Forums/en/netfxbcl/thread/f6069441-4ab1-4299-ad6a-b8bb9ed36be3
        public static Process Start(ProcessStartInfo startInfo)
        {
            Process process;

            lock (_lockObject)
            {
                using (new ChangeErrorMode(ErrorModes.FailCriticalErrors | ErrorModes.NoGpFaultErrorBox))
                {
                    process = Process.Start(startInfo);
                    process.PriorityClass = ProcessPriorityClass.Idle;
                }
            }

            return process;
        }

        // http://csharptest.net/532/using-processstart-to-capture-console-output/
        public static int Run(Action<string> output, Action<string> errorOutput, TextReader input, string exe, string args, string workingDirectory, params KeyValuePair<string, string>[] environmentalVariables)
        {
            if (String.IsNullOrEmpty(exe))
                throw new FileNotFoundException();
            if (output == null)
                throw new ArgumentNullException("output");

            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                ErrorDialog = false,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                FileName = exe,
                Arguments = args
            };
            foreach (var environmentalVariable in environmentalVariables)
            {
                psi.EnvironmentVariables.Add(environmentalVariable.Key, environmentalVariable.Value);
            }

            using (var process = Process.Start(psi))
            using (var mreOut = new ManualResetEvent(false))
            using (var mreErr = new ManualResetEvent(false))
            {
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (o, e) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    if (e.Data == null)
                        mreOut.Set();
                    else
                        output(e.Data);
                };
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (o, e) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    if (e.Data == null)
                        mreErr.Set();
                    else
                        errorOutput(e.Data);
                };
                process.BeginErrorReadLine();

                string line;
                while (input != null && null != (line = input.ReadLine()))
                    process.StandardInput.WriteLine(line);

                process.StandardInput.Close();
                process.WaitForExit();

                mreOut.WaitOne();
                mreErr.WaitOne();

                return process.ExitCode;
            }
        }

        [Flags]
        public enum ErrorModes
        {
            Default = 0x0,
            FailCriticalErrors = 0x1,
            NoGpFaultErrorBox = 0x2,
            NoAlignmentFaultExcept = 0x4,
            NoOpenFileErrorBox = 0x8000
        }

        public struct ChangeErrorMode : IDisposable
        {
            private readonly int _oldMode;

            public ChangeErrorMode(ErrorModes mode)
            {
                _oldMode = SetErrorMode((int)mode);
            }

            void IDisposable.Dispose() { SetErrorMode(_oldMode); }

            [DllImport("kernel32.dll")]
            private static extern int SetErrorMode(int newMode);
        }
    }

}