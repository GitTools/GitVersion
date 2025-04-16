using GitVersion.Helpers;

namespace GitVersion.Core.Tests.Helpers;

public static class ExecutableHelper
{
    public static string GetDotNetExecutable() => "dotnet";

    public static string GetExecutableArgs(string args) => $"{FileSystemHelper.Path.Combine(GetExeDirectory(), "gitversion.dll")} {args}";

    private static string GetExeDirectory() => FileSystemHelper.Path.GetCurrentDirectory().Replace("GitVersion.App.Tests", "GitVersion.App");
}
