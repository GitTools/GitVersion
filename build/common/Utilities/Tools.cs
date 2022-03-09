namespace Common.Utilities;

public class Tools
{
    public const string NugetCmd = "NuGet.CommandLine";

    public const string GitVersion = "GitVersion.Tool";
    public const string GitReleaseManager = "GitReleaseManager.Tool";
    public const string Codecov = "Codecov.Tool";
    public const string Wyam2 = "Wyam2.Tool";

    public static readonly Dictionary<string, string> Versions = new()
    {
        { NugetCmd, "6.0.0" },

        { GitVersion, "5.8.3" },
        { GitReleaseManager, "0.13.0" },
        { Codecov, "1.13.0" },
        { Wyam2, "3.0.0-rc3&prerelease" },
    };
}
