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
        var artifactsBinFullFxDir         = artifactsBinDir.Combine("net461");
        var artifactsBinFullFxILMergeDir  = artifactsBinFullFxDir.Combine("il-merge");
        var artifactsBinFullFxPortableDir = artifactsBinFullFxDir.Combine("portable");
        var artifactsBinFullFxCmdlineDir  = artifactsBinFullFxDir.Combine("cmdline");
        var artifactsBinNetCoreDir        = artifactsBinDir.Combine("netcoreapp2.0");
        var nugetRootDir                  = artifactsDir.Combine("nuget");
        var buildArtifactDir              = artifactsDir.Combine("build-artifact");
        var testCoverageOutputDir         = artifactsDir.Combine("code-coverage");

        var zipArtifactPathCoreClr = artifactsDir.CombineWithFilePath("GitVersion-bin-coreclr-v" + semVersion + ".zip");
        var zipArtifactPathDesktop = artifactsDir.CombineWithFilePath("GitVersion-bin-fullfx-v" + semVersion + ".zip");

        var testCoverageOutputFilePath = testCoverageOutputDir.CombineWithFilePath("TestResult.xml");

        var releaseNotesOutputFilePath = buildArtifactDir.CombineWithFilePath("releasenotes.md");
        var gemOutputFilePath  = buildArtifactDir.CombineWithFilePath("gitversion-" + version.GemVersion + ".gem");

        var tfsSuffix = parameters.IsStableRelease() ? "" : "preview-";
        var vsixOutputFilePath = buildArtifactDir.CombineWithFilePath("gittools.gitversion-" + tfsSuffix + version.TfxVersion + ".vsix");
        var vsixNetCoreOutputFilePath = buildArtifactDir.CombineWithFilePath("gittools.gitversion-netcore-" + tfsSuffix + version.TfxVersion + ".vsix");

        // Directories
        var buildDirectories = new BuildDirectories(
            artifactsDir,
            buildArtifactDir,
            testCoverageOutputDir,
            nugetRootDir,
            artifactsBinDir,
            artifactsBinFullFxDir,
            artifactsBinFullFxPortableDir,
            artifactsBinFullFxCmdlineDir,
            artifactsBinFullFxILMergeDir,
            artifactsBinNetCoreDir);

        // Files
        var buildFiles = new BuildFiles(
            context,
            zipArtifactPathCoreClr,
            zipArtifactPathDesktop,
            testCoverageOutputFilePath,
            releaseNotesOutputFilePath,
            vsixOutputFilePath,
            vsixNetCoreOutputFilePath,
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
    public FilePath TestCoverageOutputFilePath { get; private set; }
    public FilePath ReleaseNotesOutputFilePath { get; private set; }
    public FilePath VsixOutputFilePath { get; private set; }
    public FilePath VsixNetCoreOutputFilePath { get; private set; }
    public FilePath GemOutputFilePath { get; private set; }

    public BuildFiles(
        ICakeContext context,
        FilePath zipArtifactPathCoreClr,
        FilePath zipArtifactPathDesktop,
        FilePath testCoverageOutputFilePath,
        FilePath releaseNotesOutputFilePath,
        FilePath vsixOutputFilePath,
        FilePath vsixNetCoreOutputFilePath,
        FilePath gemOutputFilePath
        )
    {
        ZipArtifactPathCoreClr = zipArtifactPathCoreClr;
        ZipArtifactPathDesktop = zipArtifactPathDesktop;
        TestCoverageOutputFilePath = testCoverageOutputFilePath;
        ReleaseNotesOutputFilePath = releaseNotesOutputFilePath;
        VsixOutputFilePath = vsixOutputFilePath;
        VsixNetCoreOutputFilePath = vsixNetCoreOutputFilePath;
        GemOutputFilePath = gemOutputFilePath;
    }
}

public class BuildDirectories
{
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath NugetRoot { get; private set; }
    public DirectoryPath BuildArtifact { get; private set; }
    public DirectoryPath TestCoverageOutput { get; private set; }
    public DirectoryPath ArtifactsBin { get; private set; }
    public DirectoryPath ArtifactsBinFullFx { get; private set; }
    public DirectoryPath ArtifactsBinFullFxPortable { get; private set; }
    public DirectoryPath ArtifactsBinFullFxCmdline { get; private set; }
    public DirectoryPath ArtifactsBinFullFxILMerge { get; private set; }
    public DirectoryPath ArtifactsBinNetCore { get; private set; }
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(
        DirectoryPath artifactsDir,
        DirectoryPath buildArtifactDir,
        DirectoryPath testCoverageOutputDir,
        DirectoryPath nugetRootDir,
        DirectoryPath artifactsBinDir,
        DirectoryPath artifactsBinFullFxDir,
        DirectoryPath artifactsBinFullFxPortableDir,
        DirectoryPath artifactsBinFullFxCmdlineDir,
        DirectoryPath artifactsBinFullFxILMergeDir,
        DirectoryPath artifactsBinNetCoreDir
        )
    {
        Artifacts = artifactsDir;
        BuildArtifact = buildArtifactDir;
        TestCoverageOutput = testCoverageOutputDir;
        NugetRoot = nugetRootDir;
        ArtifactsBin = artifactsBinDir;
        ArtifactsBinFullFx = artifactsBinFullFxDir;
        ArtifactsBinFullFxPortable = artifactsBinFullFxPortableDir;
        ArtifactsBinFullFxCmdline = artifactsBinFullFxCmdlineDir;
        ArtifactsBinFullFxILMerge = artifactsBinFullFxILMergeDir;
        ArtifactsBinNetCore = artifactsBinNetCoreDir;
        ToClean = new[] {
            Artifacts,
            BuildArtifact,
            TestCoverageOutput,
            NugetRoot,
            ArtifactsBin,
            ArtifactsBinFullFx,
            ArtifactsBinFullFxPortable,
            ArtifactsBinFullFxCmdline,
            ArtifactsBinFullFxILMerge,
            ArtifactsBinNetCore
        };
    }
}
