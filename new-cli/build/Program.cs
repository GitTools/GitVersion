using Cake.Frosting;

return new CakeHost()
    .UseContext<Context>()
    .UseLifetime<ContextLifetime>()
    .UseTaskLifetime<TaskLifetime>()
    .UseWorkingDirectory("../src")
    .Run(args);