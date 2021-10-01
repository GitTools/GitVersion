global using Constants = Common.Utilities.Constants;

using Docker;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .Run(args);
