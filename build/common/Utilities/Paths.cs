namespace Common.Utilities;

public class Paths
{
    public static readonly DirectoryPath Root = "./";

    public static readonly DirectoryPath Artifacts = Root.Combine("artifacts");
    public static readonly DirectoryPath Src = Root.Combine("src");
    public static readonly DirectoryPath Docs = Root.Combine("docs");
    public static readonly DirectoryPath Build = Root.Combine("build");
    public static readonly DirectoryPath Integration = Root.Combine("tests").Combine("integration");

    public static readonly DirectoryPath Nuspec = Build.Combine("nuspec");

    public static readonly DirectoryPath TestOutput = Artifacts.Combine("test-results");
    public static readonly DirectoryPath Packages = Artifacts.Combine("packages");
    public static readonly DirectoryPath ArtifactsDocs = Artifacts.Combine("docs");

    public static readonly DirectoryPath Native = Packages.Combine("native");
    public static readonly DirectoryPath Nuget = Packages.Combine("nuget");
    public static readonly DirectoryPath ArtifactsBinCmdline = Packages.Combine("prepare").Combine("cmdline");
    public static readonly DirectoryPath ArtifactsBinPortable = Packages.Combine("prepare").Combine("portable");
}
