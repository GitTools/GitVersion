using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GitVersion.Testing;

public static partial class ProcessHelper
{
    // Guards only the Windows-global SetErrorMode state (a no-op on other platforms). It deliberately
    // does NOT serialize Process.Start: the fixtures spawn a great many short-lived git processes from
    // parallel test fixtures, so serializing spawns here would collapse that parallelism.
    private static readonly object ErrorModeLock = new();

    // http://social.msdn.microsoft.com/Forums/en/netfxbcl/thread/f6069441-4ab1-4299-ad6a-b8bb9ed36be3
    private static Process? Start(ProcessStartInfo startInfo)
    {
        Process? process;

        ChangeErrorMode errorMode;
        lock (ErrorModeLock)
        {
            errorMode = new(ErrorModes.FailCriticalErrors | ErrorModes.NoGpFaultErrorBox);
        }

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
                default:
                    throw new ArgumentOutOfRangeException($"The error code '{exception.NativeErrorCode}' is not supported.", nameof(exception));
            }

            throw;
        }
        finally
        {
            lock (ErrorModeLock)
            {
                ((IDisposable)errorMode).Dispose();
            }
        }

        return process;
    }

    // http://csharptest.net/532/using-processstart-to-capture-console-output/
    public static int Run(Action<string> output, Action<string> errorOutput, TextReader? input, string exe, string args, string workingDirectory, params KeyValuePair<string, string?>[] environmentalVariables)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exe);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args
        };
        return Run(output, errorOutput, input, psi, workingDirectory, environmentalVariables);
    }

    /// <summary>
    ///     Runs the executable passing each argument verbatim (no shell quoting required).
    /// </summary>
    public static int Run(Action<string> output, Action<string> errorOutput, TextReader? input, string exe, IReadOnlyCollection<string> args, string workingDirectory, params KeyValuePair<string, string?>[] environmentalVariables)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exe);

        var psi = new ProcessStartInfo { FileName = exe };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        return Run(output, errorOutput, input, psi, workingDirectory, environmentalVariables);
    }

    private static int Run(Action<string> output, Action<string> errorOutput, TextReader? input, ProcessStartInfo psi, string workingDirectory, params KeyValuePair<string, string?>[] environmentalVariables)
    {
        ArgumentNullException.ThrowIfNull(output);

        psi.UseShellExecute = false;
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardInput = true;
        psi.WindowStyle = ProcessWindowStyle.Hidden;
        psi.CreateNoWindow = true;
        psi.ErrorDialog = false;
        psi.WorkingDirectory = workingDirectory;
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
            {
                mreOut.Set();
            }
            else
            {
                output(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.ErrorDataReceived += (_, e) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            if (e.Data == null)
            {
                mreErr.Set();
            }
            else
            {
                errorOutput(e.Data);
            }
        };
        process.BeginErrorReadLine();

        while (input?.ReadLine() is { } line)
        {
            process.StandardInput.WriteLine(line);
        }

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
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum ErrorModes
    {
        Default = 0x0,
        FailCriticalErrors = 0x1,
        NoGpFaultErrorBox = 0x2,
        NoAlignmentFaultExcept = 0x4,
        NoOpenFileErrorBox = 0x8000
    }

    private readonly partial struct ChangeErrorMode : IDisposable
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

        [LibraryImport("kernel32.dll")]
        private static partial int SetErrorMode(int newMode);
    }
}
