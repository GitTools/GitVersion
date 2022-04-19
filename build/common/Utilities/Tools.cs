namespace Common.Utilities;

public class Tools
{
    public const string NugetCmd = "NuGet.CommandLine";

    public static readonly Dictionary<string, string> Versions = new()
    {
        { NugetCmd, "6.1.0" },
    };
}
