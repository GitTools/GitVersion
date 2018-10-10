public class BuildPackages
{
    public ICollection<BuildPackage> All { get; private set; }
    public ICollection<BuildPackage> Nuget { get; private set; }
    public ICollection<BuildPackage> Chocolatey { get; private set; }

    public static BuildPackages GetPackages(
        DirectoryPath nugetRooPath,
        string semVersion,
        string[] packageIds,
        string[] chocolateyPackageIds)
    {
        var toNugetPackage = BuildPackage(nugetRooPath, semVersion);
        var toChocolateyPackage = BuildPackage(nugetRooPath, semVersion, isChocolateyPackage: true);
        var nugetPackages = packageIds.Select(toNugetPackage).ToArray();
        var chocolateyPackages = chocolateyPackageIds.Select(toChocolateyPackage).ToArray();

        return new BuildPackages {
            All = nugetPackages.Union(chocolateyPackages).ToArray(),
            Nuget = nugetPackages,
            Chocolatey = chocolateyPackages
        };
    }

    private static Func<string, BuildPackage> BuildPackage(
        DirectoryPath nugetRooPath,
        string semVersion,
        bool isChocolateyPackage = false)
    {
        return package => new BuildPackage(
            id: package,
            nuspecPath: string.Concat("./nuspec/", package, ".nuspec"),
            packagePath: nugetRooPath.CombineWithFilePath(string.Concat(package, ".", semVersion, ".nupkg")),
            isChocolateyPackage: isChocolateyPackage);
    }
}

public class BuildPackage
{
    public string Id { get; private set; }
    public FilePath NuspecPath { get; private set; }
    public FilePath PackagePath { get; private set; }
    public bool IsChocolateyPackage { get; private set; }
    public string PackageName { get; private set; }


    public BuildPackage(
        string id,
        FilePath nuspecPath,
        FilePath packagePath,
        bool isChocolateyPackage)
    {
        Id = id;
        NuspecPath = nuspecPath;
        PackagePath = packagePath;
        IsChocolateyPackage = isChocolateyPackage;
        PackageName = PackagePath.GetFilename().ToString();
    }
}

public class BuildArtifacts
{
    public ICollection<BuildArtifact> All { get; private set; }

    public static BuildArtifacts GetArtifacts(FilePath[] artifacts)
    {
        var toBuildArtifact = BuildArtifact("build-artifact");
        var buildArtifacts = artifacts.Select(toBuildArtifact).ToArray();

        return new BuildArtifacts {
            All = buildArtifacts.ToArray(),
        };
    }

    private static Func<FilePath, BuildArtifact> BuildArtifact(string containerName)
    {
        return artifactPath => new BuildArtifact(containerName: containerName, artifactPath: artifactPath);
    }
}

public class BuildArtifact
{
    public string ContainerName { get; private set; }
    public FilePath ArtifactPath { get; private set; }
    public string ArtifactName { get; private set; }

    public BuildArtifact(
        string containerName,
        FilePath artifactPath)
    {
        ContainerName = containerName;
        ArtifactPath = artifactPath.FullPath;
        ArtifactName = ArtifactPath.GetFilename().ToString();
    }
}
