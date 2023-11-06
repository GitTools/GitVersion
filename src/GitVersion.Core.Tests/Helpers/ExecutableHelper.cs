using GitVersion.Helpers;

namespace GitVersion.Core.Tests.Helpers;

public static class ExecutableHelper
{
    public static string GetDotNetExecutable() => "dotnet";

    public static string GetExecutableArgs(string args) => $"{PathHelper.Combine(GetExeDirectory(), "gitversion.dll")} {args}";

    private static string GetExeDirectory() => PathHelper.GetCurrentDirectory().Replace("GitVersion.App.Tests", "GitVersion.App");
}
