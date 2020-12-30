using Cake.Frosting;

return new CakeHost()
    .UseContext<Context>()
    .UseLifetime<Lifetime>()
    .UseTaskLifetime<TaskLifetime>()
    .UseWorkingDirectory("../src")
    .Run(args);