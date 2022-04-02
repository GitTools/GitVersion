using GitVersion.Helpers;

namespace GitVersion.Core.Tests.Helpers;

public static class ExecutableHelper
{
    public static string GetCurrentDirectory() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException();

    public static string GetExecutable() => RuntimeHelper.IsCoreClr() ? "dotnet" : PathHelper.Combine(GetExeDirectory(), "gitversion.exe");

    public static string GetExecutableArgs(string args)
    {
        if (RuntimeHelper.IsCoreClr())
        {
            args = $"{PathHelper.Combine(GetExeDirectory(), "gitversion.dll")} {args}";
        }
        return args;
    }

    public static string GetTempPath() => PathHelper.Combine(GetCurrentDirectory(), "TestRepositories", Guid.NewGuid().ToString());

    private static string GetExeDirectory() => GetCurrentDirectory().Replace("GitVersion.App.Tests", "GitVersion.App");
}
