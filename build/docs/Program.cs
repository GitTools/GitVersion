global using Constants = Common.Utilities.Constants;

using Docs;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .Run(args);
