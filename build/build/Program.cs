using Build;
using Cake.Frosting;
using Common.Lifetime;
using Common.Utilities;

new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseWorkingDirectory(CommonPaths.WorkingDirectory)
    .SetToolPath(CommonPaths.ToolsDirectory)
    .Run(args);
