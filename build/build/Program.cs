using Build;
using Common.Lifetime;
using Common.Utilities;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseRootDirectory()
    .InstallToolsFromRootManifest()
    .InstallNugetTool(Tools.CodecovUploaderCmd, Tools.Versions[Tools.CodecovUploaderCmd])
    .Run(args);
