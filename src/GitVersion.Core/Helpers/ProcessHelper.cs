using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GitVersion.Helpers;

internal static class ProcessHelper
{
    private static readonly object LockObject = new();

    // http://social.msdn.microsoft.com/Forums/en/netfxbcl/thread/f6069441-4ab1-4299-ad6a-b8bb9ed36be3
    private static Process? Start(ProcessStartInfo startInfo)
    {
        Process? process;

        lock (LockObject)
        {
            using (new ChangeErrorMode(ErrorModes.FailCriticalErrors | ErrorModes.NoGpFaultErrorBox))
            {
                try
                {
                    process = Process.Start(startInfo);
                }
                catch (Win32Exception exception)
                {
                    switch ((NativeErrorCode)exception.NativeErrorCode)
                    {
                        case NativeErrorCode.Success:
                            // Success is not a failure.
                            break;

                        case NativeErrorCode.FileNotFound:
                            throw new FileNotFoundException($"The executable file '{startInfo.FileName}' could not be found.",
                                startInfo.FileName,
                                exception);

                        case NativeErrorCode.PathNotFound:
                            throw new DirectoryNotFoundException($"The path to the executable file '{startInfo.FileName}' could not be found.",
                                exception);
                    }

                    throw;
                }

                try
                {
                    if (process != null)
                    {
                        process.PriorityClass = ProcessPriorityClass.Idle;
                    }
                }
                catch
                {
                    // NOTE: It seems like in some situations, setting the priority class will throw a Win32Exception
                    // with the error code set to "Success", which I think we can safely interpret as a success and
                    // not an exception.
                    //
                    // See: https://travis-ci.org/GitTools/GitVersion/jobs/171288284#L2026
                    // And: https://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
                    //
                    // There's also the case where the process might be killed before we try to adjust its priority
                    // class, in which case it will throw an InvalidOperationException. What we ideally should do
                    // is start the process in a "suspended" state, adjust the priority class, then resume it, but
                    // that's not possible in pure .NET.
                    //
                    // See: https://travis-ci.org/GitTools/GitVersion/jobs/166709203#L2278
                    // And: http://www.codeproject.com/Articles/230005/Launch-a-process-suspended
                    //
                    // -- @asbjornu
                }
            }
        }

        return process;
    }

    // http://csharptest.net/532/using-processstart-to-capture-console-output/
    public static int Run(Action<string> output, Action<string> errorOutput, TextReader? input, string exe, string args, string workingDirectory, params KeyValuePair<string, string?>[] environmentalVariables)
    {
        if (string.IsNullOrEmpty(exe))
            throw new ArgumentNullException(nameof(exe));
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            ErrorDialog = false,
            WorkingDirectory = workingDirectory,
            FileName = exe,
            Arguments = args
        };
        foreach (var (key, value) in environmentalVariables)
        {
            var psiEnvironmentVariables = psi.EnvironmentVariables;
            if (psiEnvironmentVariables.ContainsKey(key) && string.IsNullOrEmpty(value))
            {
                psiEnvironmentVariables.Remove(key);
            }
            else if (psiEnvironmentVariables.ContainsKey(key))
            {
                psiEnvironmentVariables[key] = value;
            }
            else
            {
                psiEnvironmentVariables.Add(key, value);
            }
        }

        using var process = Start(psi);

        if (process is null)
        {
            // FIX ME: What error code do you want to return?
            return -1;
        }

        using var mreOut = new ManualResetEvent(false);
        using var mreErr = new ManualResetEvent(false);
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, e) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            if (e.Data == null)
                mreOut.Set();
            else
                output(e.Data);
        };
        process.BeginOutputReadLine();
        process.ErrorDataReceived += (_, e) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            if (e.Data == null)
                mreErr.Set();
            else
                errorOutput(e.Data);
        };
        process.BeginErrorReadLine();

        string? line;
        while ((line = input?.ReadLine()) != null)
            process.StandardInput.WriteLine(line);

        process.StandardInput.Close();
        process.WaitForExit();

        mreOut.WaitOne();
        mreErr.WaitOne();

        return process.ExitCode;
    }

    /// <summary>
    /// System error codes.
    /// See: https://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
    /// </summary>
    private enum NativeErrorCode
    {
        Success = 0x0,
        FileNotFound = 0x2,
        PathNotFound = 0x3
    }

    [Flags]
    private enum ErrorModes
    {
        Default = 0x0,
        FailCriticalErrors = 0x1,
        NoGpFaultErrorBox = 0x2,
        NoAlignmentFaultExcept = 0x4,
        NoOpenFileErrorBox = 0x8000
    }

    private readonly struct ChangeErrorMode : IDisposable
    {
        private readonly int oldMode;

        public ChangeErrorMode(ErrorModes mode)
        {
            try
            {
                this.oldMode = SetErrorMode((int)mode);
            }
            catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
            {
                this.oldMode = (int)mode;
            }
        }

        void IDisposable.Dispose()
        {
            try
            {
                _ = SetErrorMode(this.oldMode);
            }
            catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
            {
                // NOTE: Mono doesn't support DllImport("kernel32.dll") and its SetErrorMode method, obviously. @asbjornu
            }
        }

        [DllImport("kernel32.dll")]
        private static extern int SetErrorMode(int newMode);
    }
}
