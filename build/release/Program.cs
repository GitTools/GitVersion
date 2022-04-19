using Common.Lifetime;
using Common.Utilities;
using Release;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseRootDirectory()
    .InstallToolsFromRootManifest()
    .Run(args);
