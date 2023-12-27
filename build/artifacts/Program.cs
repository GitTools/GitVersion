using Artifacts;
using Common.Lifetime;
using Common.Utilities;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseRootDirectory()
    .Run(args);
