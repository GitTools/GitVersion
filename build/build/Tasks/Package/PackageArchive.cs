using Cake.Compression;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(PackageArchive))]
[TaskDescription("Creates the tar.gz or zip packages")]
[IsDependentOn(typeof(PackagePrepare))]
public class PackageArchive : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists(Paths.Native);

        var platform = context.Environment.Platform.Family;
        var runtimes = context.NativeRuntimes[platform];

        foreach (var runtime in runtimes)
        {
            var sourceDir = Paths.Native.Combine(platform.ToString().ToLower()).Combine(runtime);
            var targetDir = Paths.Native;
            context.EnsureDirectoryExists(targetDir);

            var archive = GetArchiveOutputPath(context, runtime, platform, targetDir);
            var filePaths = context.GetFiles($"{sourceDir}/**/*");
            switch (platform)
            {
                case PlatformFamily.Windows:
                    context.ZipCompress(sourceDir, archive, filePaths);
                    break;
                default:
                    context.GZipCompress(sourceDir, archive, filePaths);
                    break;
            }

            context.Information($"Created {archive}");
        }
        base.Run(context);
    }
    private static FilePath GetArchiveOutputPath(BuildContextBase context, string runtime, PlatformFamily platform, DirectoryPath targetDir)
    {
        var ext = platform == PlatformFamily.Windows ? "zip" : "tar.gz";
        var fileName = $"gitversion-{runtime}-{context.Version?.SemVersion}.{ext}".ToLower();
        return targetDir.CombineWithFilePath(fileName);
    }
}
