using Cake.Frosting;
using Common.Utilities;
using Release;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .SetToolPath(Paths.ToolsDirectory)
    .Run(args);
