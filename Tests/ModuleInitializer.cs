using System;
using System.Diagnostics;
using System.IO;
using GitFlowVersion;

/// <summary>
/// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
/// </summary>
public static class ModuleInitializer
{
    /// <summary>
    /// Initializes the module.
    /// </summary>
    public static void Initialize()
    {
        Logger.WriteInfo = s => Trace.WriteLine(s);
        var nativeBinaries = Path.Combine(AssemblyLocation.CurrentDirectory(), "NativeBinaries", GetProcessorArchitecture());
        var existingPath = Environment.GetEnvironmentVariable("PATH");
        if (existingPath.Contains(nativeBinaries))
        {
            return;
        }
        var newPath = string.Concat(nativeBinaries, Path.PathSeparator, existingPath);
        Environment.SetEnvironmentVariable("PATH", newPath);
    }

    static string GetProcessorArchitecture()
    {
        if (Environment.Is64BitProcess)
        {
            return "amd64";
        }
        return "x86";
    }
}