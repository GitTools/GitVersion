using Cake.Frosting;
using Chores;
using Common.Lifetime;
using Common.Utilities;

new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseWorkingDirectory(CommonPaths.WorkingDirectory)
    .SetToolPath(CommonPaths.ToolsDirectory)
    .Run(args);
