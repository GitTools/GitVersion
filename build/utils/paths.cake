public class BuildPaths
{
    public BuildFiles Files { get; private set; }
    public BuildDirectories Directories { get; private set; }

    public static BuildPaths GetPaths(
        ICakeContext context,
        BuildParameters parameters,
        string configuration,
        BuildVersion version
        )
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        if (string.IsNullOrEmpty(configuration))
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        var semVersion = version.SemVersion;

        var sourceDir                     = (DirectoryPath)(context.Directory("./src"));
        var artifactsDir                  = (DirectoryPath)(context.Directory("./artifacts") + context.Directory("v" + semVersion));
        var artifactsBinDir               = artifactsDir.Combine("bin");
        var artifactsBinPortableDir       = artifactsBinDir.Combine("portable");
        var artifactsBinCmdlineDir        = artifactsBinDir.Combine("cmdline");
        var artifactsBinCoreFx21Dir       = artifactsBinDir.Combine(parameters.CoreFxVersion21);
        var artifactsBinCoreFx30Dir       = artifactsBinDir.Combine(parameters.CoreFxVersion31);
        var artifactsBinFullFx472Dir      = artifactsBinDir.Combine(parameters.FullFxVersion472);
        var nativeDir                     = artifactsDir.Combine("native");
        var nugetRootDir                  = artifactsDir.Combine("nuget");
        var buildArtifactDir              = artifactsDir.Combine("build-artifact");
        var testResultsOutputDir          = artifactsDir.Combine("test-results");

        var releaseNotesOutputFilePath = buildArtifactDir.CombineWithFilePath("releasenotes.md");
        var gemOutputFilePath  = buildArtifactDir.CombineWithFilePath("gitversion-" + version.GemVersion + ".gem");

        // Directories
        var buildDirectories = new BuildDirectories(
            sourceDir,
            artifactsDir,
            nativeDir,
            buildArtifactDir,
            testResultsOutputDir,
            nugetRootDir,
            artifactsBinDir,
            artifactsBinPortableDir,
            artifactsBinCmdlineDir,
            artifactsBinCoreFx21Dir,
            artifactsBinCoreFx30Dir,
            artifactsBinFullFx472Dir);

        // Files
        var buildFiles = new BuildFiles(
            context,
            releaseNotesOutputFilePath,
            gemOutputFilePath);

        return new BuildPaths
        {
            Files = buildFiles,
            Directories = buildDirectories
        };
    }
}

public class BuildFiles
{
    public FilePath ReleaseNotesOutputFilePath { get; private set; }
    public FilePath GemOutputFilePath { get; private set; }

    public BuildFiles(
        ICakeContext context,
        FilePath releaseNotesOutputFilePath,
        FilePath gemOutputFilePath
        )
    {
        ReleaseNotesOutputFilePath = releaseNotesOutputFilePath;
        GemOutputFilePath = gemOutputFilePath;
    }
}

public class BuildDirectories
{
    public DirectoryPath Source { get; private set; }
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath Native { get; private set; }
    public DirectoryPath NugetRoot { get; private set; }
    public DirectoryPath BuildArtifact { get; private set; }
    public DirectoryPath TestResultsOutput { get; private set; }
    public DirectoryPath ArtifactsBin { get; private set; }
    public DirectoryPath ArtifactsBinPortable { get; private set; }
    public DirectoryPath ArtifactsBinCmdline { get; private set; }
    public DirectoryPath ArtifactsBinCoreFx21 { get; private set; }
    public DirectoryPath ArtifactsBinCoreFx30 { get; private set; }
    public DirectoryPath ArtifactsBinFullFx472 { get; private set; }
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(
        DirectoryPath sourceDir,
        DirectoryPath artifactsDir,
        DirectoryPath nativeDir,
        DirectoryPath buildArtifactDir,
        DirectoryPath testResultsOutputDir,
        DirectoryPath nugetRootDir,
        DirectoryPath artifactsBinDir,
        DirectoryPath artifactsBinPortableDir,
        DirectoryPath artifactsBinCmdlineDir,
        DirectoryPath artifactsBinCoreFx21Dir,
        DirectoryPath artifactsBinCoreFx30Dir,
        DirectoryPath artifactsBinFullFx472Dir
        )
    {
        Source = sourceDir;
        Artifacts = artifactsDir;
        Native = nativeDir;
        BuildArtifact = buildArtifactDir;
        TestResultsOutput = testResultsOutputDir;
        NugetRoot = nugetRootDir;
        ArtifactsBin = artifactsBinDir;
        ArtifactsBinPortable = artifactsBinPortableDir;
        ArtifactsBinCmdline = artifactsBinCmdlineDir;
        ArtifactsBinCoreFx21 = artifactsBinCoreFx21Dir;
        ArtifactsBinCoreFx30 = artifactsBinCoreFx30Dir;
        ArtifactsBinFullFx472 = artifactsBinFullFx472Dir;
        ToClean = new[] {
            Artifacts,
            Native,
            BuildArtifact,
            TestResultsOutput,
            NugetRoot,
            ArtifactsBin,
            ArtifactsBinPortable,
            ArtifactsBinCmdline,
            ArtifactsBinCoreFx21,
            ArtifactsBinCoreFx30,
            ArtifactsBinFullFx472
        };
    }
}
