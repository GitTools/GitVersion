using Build;
using Cake.Frosting;
using Common.Utilities;

new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .UseWorkingDirectory(CommonPaths.WorkingDirectory)
    .SetToolPath(CommonPaths.ToolsDirectory)
    .Run(args);
