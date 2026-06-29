using GitVersion.Helpers;

namespace GitVersion.App.Tests.Helpers;

public static class ExecutableHelper
{
    public const string DotNetExecutable = "dotnet";

    public static string GetExecutableArgs(string args) => $"{FileSystemHelper.Path.Combine(GetExeDirectory(), "gitversion.dll")} {args}";

    private static string GetExeDirectory() => FileSystemHelper.Path.GetCurrentDirectory().Replace("GitVersion.App.Tests", "GitVersion.App");
}
