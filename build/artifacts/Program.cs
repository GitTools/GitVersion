using Artifacts;

return new CakeHost()
    .UseContext<BuildContext>()
    .UseStartup<Startup>()
    .Run(args);
