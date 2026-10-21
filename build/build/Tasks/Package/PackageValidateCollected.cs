using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(PackageValidateCollected))]
[TaskDescription("Validates the complete collected native tool and Linux archive set")]
[TaskArgument(Arguments.PackageDirectory)]
public sealed class PackageValidateCollected : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var root = new DirectoryPath(context.Argument(Arguments.PackageDirectory, Paths.Packages.FullPath));
        var packages = context.GetFiles($"{root}/nuget/*.nupkg");
        var packageIds = new[] { "GitVersion.Tool" }.Concat(context.NativeRuntimes.Values
            .SelectMany(runtimes => runtimes).Select(runtime => $"GitVersion.Tool.{runtime}"));
        foreach (var id in packageIds)
        {
            var prefix = id + ".";
            var count = packages.Count(package =>
            {
                var name = package.GetFilename().FullPath;
                return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && name.Length > prefix.Length && char.IsAsciiDigit(name[prefix.Length]);
            });
            if (count != 1)
            {
                throw new CakeException($"Expected one package for {id}; found {count}.");
            }
        }
        foreach (var runtime in context.NativeRuntimes[PlatformFamily.Linux])
        {
            var count = context.GetFiles($"{root}/native/gitversion-{runtime}-*.tar.gz").Count;
            if (count != 1)
            {
                throw new CakeException($"Expected one archive for {runtime}; found {count}.");
            }
        }
    }
}
