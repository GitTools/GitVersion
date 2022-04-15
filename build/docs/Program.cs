using Common.Lifetime;
using Common.Utilities;
using Docs;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseRootDirectory()
    .InstallToolsFromRootManifest()
    .Run(args);
