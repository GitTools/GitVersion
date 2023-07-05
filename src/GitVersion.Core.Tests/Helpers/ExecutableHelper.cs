using GitVersion.Helpers;

namespace GitVersion.Core.Tests.Helpers;

public static class ExecutableHelper
{
    public static string GetCurrentDirectory() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException();

    public static string GetDotNetExecutable() => "dotnet";

    public static string GetExecutableArgs(string args) => $"{PathHelper.Combine(GetExeDirectory(), "gitversion.dll")} {args}";

    public static string GetTempPath() => PathHelper.Combine(GetCurrentDirectory(), "TestRepositories", Guid.NewGuid().ToString());

    private static string GetExeDirectory() => GetCurrentDirectory().Replace("GitVersion.App.Tests", "GitVersion.App");
}
