using Cake.Frosting;
using Common.Utilities;
using Docker;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .SetToolPath(Paths.ToolsDirectory)
    .Run(args);
