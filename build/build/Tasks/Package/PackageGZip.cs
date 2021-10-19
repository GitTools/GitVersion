using Cake.Compression;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(PackageGZip))]
[TaskDescription("Creates the tar.gz packages")]
[IsDependentOn(typeof(PackagePrepare))]
public class PackageGZip : FrostingTask<BuildContext>
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

            var fileName = $"gitversion-{runtime}-{context.Version?.SemVersion}.tar.gz".ToLower();
            var tarFile = targetDir.CombineWithFilePath(fileName);
            var filePaths = context.GetFiles($"{sourceDir}/**/*");
            context.GZipCompress(sourceDir, tarFile, filePaths);

            context.Information($"Created {tarFile}");
        }
        base.Run(context);
    }
}
