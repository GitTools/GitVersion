public class BuildPaths
{
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

        var rootDir                       = (DirectoryPath)(context.Directory("."));
        var sourceDir                     = rootDir.Combine("src");
        var artifactsRootDir              = rootDir.Combine("artifacts");
        var artifactsDir                  = artifactsRootDir.Combine("v" + semVersion);
        var artifactsBinDir               = artifactsDir.Combine("bin");
        var artifactsBinPortableDir       = artifactsBinDir.Combine("portable");
        var artifactsBinCmdlineDir        = artifactsBinDir.Combine("cmdline");
        var nativeDir                     = artifactsDir.Combine("native");
        var nugetRootDir                  = artifactsDir.Combine("nuget");
        var buildArtifactDir              = artifactsDir.Combine("build-artifact");
        var testResultsOutputDir          = artifactsDir.Combine("test-results");

        var releaseNotesOutputFilePath = buildArtifactDir.CombineWithFilePath("releasenotes.md");

        // Directories
        var buildDirectories = new BuildDirectories(
            rootDir,
            sourceDir,
            artifactsRootDir,
            artifactsDir,
            nativeDir,
            buildArtifactDir,
            testResultsOutputDir,
            nugetRootDir,
            artifactsBinDir,
            artifactsBinPortableDir,
            artifactsBinCmdlineDir);

        return new BuildPaths
        {
            Directories = buildDirectories
        };
    }
}

public class BuildDirectories
{
    public DirectoryPath Root { get; private set; }
    public DirectoryPath Source { get; private set; }
    public DirectoryPath ArtifactsRoot { get; private set; }
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath Native { get; private set; }
    public DirectoryPath NugetRoot { get; private set; }
    public DirectoryPath BuildArtifact { get; private set; }
    public DirectoryPath TestResultsOutput { get; private set; }
    public DirectoryPath ArtifactsBin { get; private set; }
    public DirectoryPath ArtifactsBinPortable { get; private set; }
    public DirectoryPath ArtifactsBinCmdline { get; private set; }
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(
        DirectoryPath rootDir,
        DirectoryPath sourceDir,
        DirectoryPath artifactsRootDir,
        DirectoryPath artifactsDir,
        DirectoryPath nativeDir,
        DirectoryPath buildArtifactDir,
        DirectoryPath testResultsOutputDir,
        DirectoryPath nugetRootDir,
        DirectoryPath artifactsBinDir,
        DirectoryPath artifactsBinPortableDir,
        DirectoryPath artifactsBinCmdlineDir
        )
    {
        Root = rootDir;
        Source = sourceDir;
        ArtifactsRoot = artifactsRootDir;
        Artifacts = artifactsDir;
        Native = nativeDir;
        BuildArtifact = buildArtifactDir;
        TestResultsOutput = testResultsOutputDir;
        NugetRoot = nugetRootDir;
        ArtifactsBin = artifactsBinDir;
        ArtifactsBinPortable = artifactsBinPortableDir;
        ArtifactsBinCmdline = artifactsBinCmdlineDir;
        ToClean = new[] {
            Artifacts,
            Native,
            BuildArtifact,
            TestResultsOutput,
            NugetRoot,
            ArtifactsBin,
            ArtifactsBinPortable,
            ArtifactsBinCmdline
        };
    }
}
