using Build;
using Cake.Frosting;
using Common.Utilities;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .SetToolPath(Paths.ToolsDirectory)
    .Run(args);
