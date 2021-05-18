using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core.IO;
using Common.Utilities;

namespace Build.Utilities
{
    public class BuildPackages
    {
        public ICollection<BuildPackage>? All { get; private set; }
        public ICollection<BuildPackage>? Nuget { get; private set; }
        public ICollection<BuildPackage>? Chocolatey { get; private set; }

        public static BuildPackages GetPackages(
            DirectoryPath nugetRooPath,
            BuildVersion version,
            IEnumerable<string> packageIds,
            IEnumerable<string> chocolateyPackageIds)
        {
            var toNugetPackage = BuildPackage(nugetRooPath, version.NugetVersion);
            var toChocolateyPackage = BuildPackage(nugetRooPath, version.SemVersion, true);
            var nugetPackages = packageIds.Select(toNugetPackage).ToArray();
            var chocolateyPackages = chocolateyPackageIds.Select(toChocolateyPackage).ToArray();

            return new BuildPackages
            {
                All = nugetPackages.Union(chocolateyPackages).ToArray(),
                Nuget = nugetPackages,
                Chocolatey = chocolateyPackages
            };
        }

        private static Func<string, BuildPackage> BuildPackage(
            DirectoryPath nugetRooPath,
            string? version,
            bool isChocolateyPackage = false)
        {
            return package => new BuildPackage(
                Id: package,
                NuspecPath: string.Concat("./build/nuspec/", package, ".nuspec"),
                PackagePath: nugetRooPath.CombineWithFilePath(string.Concat(package, ".", version, ".nupkg")),
                IsChocolateyPackage: isChocolateyPackage);
        }
    }

    public record BuildPackage(string Id, FilePath NuspecPath, FilePath PackagePath, bool IsChocolateyPackage);
}
