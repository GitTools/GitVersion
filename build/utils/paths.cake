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

        var artifactsDir                  = (DirectoryPath)(context.Directory("./artifacts") + context.Directory("v" + semVersion));
        var artifactsBinDir               = artifactsDir.Combine("bin");
        var artifactsBinFullFxDir         = artifactsBinDir.Combine(parameters.FullFxVersion);
        var artifactsBinFullFxILMergeDir  = artifactsBinFullFxDir.Combine("il-merge");
        var artifactsBinFullFxPortableDir = artifactsBinFullFxDir.Combine("portable");
        var artifactsBinFullFxCmdlineDir  = artifactsBinFullFxDir.Combine("cmdline");
        var artifactsBinCoreFxDir         = artifactsBinDir.Combine(parameters.CoreFxVersion);
        var nugetRootDir                  = artifactsDir.Combine("nuget");
        var buildArtifactDir              = artifactsDir.Combine("build-artifact");
        var testResultsOutputDir         = artifactsDir.Combine("test-results");

        var zipArtifactPathCoreClr = artifactsDir.CombineWithFilePath("GitVersion-bin-corefx-v" + semVersion + ".zip");
        var zipArtifactPathDesktop = artifactsDir.CombineWithFilePath("GitVersion-bin-fullfx-v" + semVersion + ".zip");

        var releaseNotesOutputFilePath = buildArtifactDir.CombineWithFilePath("releasenotes.md");
        var gemOutputFilePath  = buildArtifactDir.CombineWithFilePath("gitversion-" + version.GemVersion + ".gem");

        var vsixSuffix = parameters.IsStableRelease() ? "" : "preview-";
        var vsixOutputFilePath = buildArtifactDir.CombineWithFilePath("gittools.gitversion-" + vsixSuffix + version.VsixVersion + ".vsix");

        // Directories
        var buildDirectories = new BuildDirectories(
            artifactsDir,
            buildArtifactDir,
            testResultsOutputDir,
            nugetRootDir,
            artifactsBinDir,
            artifactsBinFullFxDir,
            artifactsBinFullFxPortableDir,
            artifactsBinFullFxCmdlineDir,
            artifactsBinFullFxILMergeDir,
            artifactsBinCoreFxDir);

        // Files
        var buildFiles = new BuildFiles(
            context,
            zipArtifactPathCoreClr,
            zipArtifactPathDesktop,
            releaseNotesOutputFilePath,
            vsixOutputFilePath,
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
    public FilePath ZipArtifactPathCoreClr { get; private set; }
    public FilePath ZipArtifactPathDesktop { get; private set; }
    public FilePath ReleaseNotesOutputFilePath { get; private set; }
    public FilePath VsixOutputFilePath { get; private set; }
    public FilePath GemOutputFilePath { get; private set; }

    public BuildFiles(
        ICakeContext context,
        FilePath zipArtifactPathCoreClr,
        FilePath zipArtifactPathDesktop,
        FilePath releaseNotesOutputFilePath,
        FilePath vsixOutputFilePath,
        FilePath gemOutputFilePath
        )
    {
        ZipArtifactPathCoreClr = zipArtifactPathCoreClr;
        ZipArtifactPathDesktop = zipArtifactPathDesktop;
        ReleaseNotesOutputFilePath = releaseNotesOutputFilePath;
        VsixOutputFilePath = vsixOutputFilePath;
        GemOutputFilePath = gemOutputFilePath;
    }
}

public class BuildDirectories
{
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath NugetRoot { get; private set; }
    public DirectoryPath BuildArtifact { get; private set; }
    public DirectoryPath TestResultsOutput { get; private set; }
    public DirectoryPath ArtifactsBin { get; private set; }
    public DirectoryPath ArtifactsBinFullFx { get; private set; }
    public DirectoryPath ArtifactsBinFullFxPortable { get; private set; }
    public DirectoryPath ArtifactsBinFullFxCmdline { get; private set; }
    public DirectoryPath ArtifactsBinFullFxILMerge { get; private set; }
    public DirectoryPath ArtifactsBinCoreFx { get; private set; }
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(
        DirectoryPath artifactsDir,
        DirectoryPath buildArtifactDir,
        DirectoryPath testResultsOutputDir,
        DirectoryPath nugetRootDir,
        DirectoryPath artifactsBinDir,
        DirectoryPath artifactsBinFullFxDir,
        DirectoryPath artifactsBinFullFxPortableDir,
        DirectoryPath artifactsBinFullFxCmdlineDir,
        DirectoryPath artifactsBinFullFxILMergeDir,
        DirectoryPath artifactsBinCoreFxDir
        )
    {
        Artifacts = artifactsDir;
        BuildArtifact = buildArtifactDir;
        TestResultsOutput = testResultsOutputDir;
        NugetRoot = nugetRootDir;
        ArtifactsBin = artifactsBinDir;
        ArtifactsBinFullFx = artifactsBinFullFxDir;
        ArtifactsBinFullFxPortable = artifactsBinFullFxPortableDir;
        ArtifactsBinFullFxCmdline = artifactsBinFullFxCmdlineDir;
        ArtifactsBinFullFxILMerge = artifactsBinFullFxILMergeDir;
        ArtifactsBinCoreFx = artifactsBinCoreFxDir;
        ToClean = new[] {
            Artifacts,
            BuildArtifact,
            TestResultsOutput,
            NugetRoot,
            ArtifactsBin,
            ArtifactsBinFullFx,
            ArtifactsBinFullFxPortable,
            ArtifactsBinFullFxCmdline,
            ArtifactsBinFullFxILMerge,
            ArtifactsBinCoreFx
        };
    }
}
