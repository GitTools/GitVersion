using Build;
using Common.Lifetime;
using Common.Utilities;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseRootDirectory()
    .InstallToolsFromRootManifest()
    .InstallNugetTool(Tools.NugetCmd, Tools.Versions[Tools.NugetCmd])
    .Run(args);
