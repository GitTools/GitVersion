global using Constants = Common.Utilities.Constants;

using Publish;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .Run(args);
